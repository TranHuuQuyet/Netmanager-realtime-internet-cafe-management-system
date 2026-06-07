using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Shared.DTOs.RequestPayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Networking;
using ServerApp.Auth.Services;
namespace ServerApp.Networking;

public sealed class TcpJsonLineServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly PacketDispatcher _dispatcher;
    private readonly ISessionService _sessions;
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly ConcurrentDictionary<string, string> _sessionBindings = new();
    private readonly ConcurrentDictionary<string, string> _machineBindings = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private Task? _acceptLoopTask;
    private int _nextClientNumber;
    private bool _isStarted;

    public TcpJsonLineServer(
        IPAddress address,
        int port,
        PacketDispatcher dispatcher,
        ISessionService sessions)
    {
        _listener = new TcpListener(address, port);
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public bool IsStarted => _isStarted;

    public IPEndPoint LocalEndpoint => (IPEndPoint)_listener.LocalEndpoint;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _listener.Start();
        _isStarted = true;
        _acceptLoopTask = AcceptLoopAsync(_stopTokenSource.Token);
    }

    public event Action<NetworkTraceEntry>? TraceEmitted;

    public event Action<StatusPayload>? StatusEmitted;

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                string clientId = $"tcp-{Interlocked.Increment(ref _nextClientNumber):0000}";
                var connection = new ClientConnection(clientId, tcpClient);

                if (!_connections.TryAdd(clientId, connection))
                {
                    connection.Dispose();
                    continue;
                }

                TraceEmitted?.Invoke(new NetworkTraceEntry("CONNECTED", clientId, string.Empty));
                connection.MessageReceived += ClientMessageReceived;
                connection.Disconnected += ClientDisconnected;
                _ = connection.ReceiveLoopAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("SERVER_ERROR", string.Empty, ex.Message));
        }
    }

    private void ClientMessageReceived(ClientConnection connection, string message)
    {
        _ = HandleClientMessageAsync(connection, message, _stopTokenSource.Token);
    }

    private async Task HandleClientMessageAsync(
        ClientConnection connection,
        string message,
        CancellationToken cancellationToken)
    {
        TraceEmitted?.Invoke(new NetworkTraceEntry("IN", connection.ClientId, message));

        try
        {
            PacketDispatchResult result = await _dispatcher.DispatchAsync(message, cancellationToken).ConfigureAwait(false);

            if (result.OpenedSessionId is not null)
            {
                if (!_sessionBindings.TryAdd(connection.ClientId, result.OpenedSessionId))
                {
                    await _sessions.CloseSessionAsync(result.OpenedSessionId, cancellationToken).ConfigureAwait(false);
                    connection.Disconnect();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result.MachineId))
                {
                    string machineId = result.MachineId.Trim();
                    _machineBindings[connection.ClientId] = machineId;
                    EmitStatus(connection.ClientId, machineId, "Online");
                }
            }

            TraceEmitted?.Invoke(new NetworkTraceEntry("OUT", connection.ClientId, result.Response));
            await connection.SendAsync(result.Response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or JsonException)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("DISPATCH_ERROR", connection.ClientId, ex.Message));
            connection.Disconnect();
        }
    }

    private void ClientDisconnected(ClientConnection connection)
    {
        _connections.TryRemove(connection.ClientId, out _);

        if (_sessionBindings.TryRemove(connection.ClientId, out var sessionId))
        {
            try
            {
                _sessions.CloseSessionAsync(sessionId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                TraceEmitted?.Invoke(new NetworkTraceEntry("SESSION_ERROR", connection.ClientId, ex.Message));
            }
        }

        if (_machineBindings.TryRemove(connection.ClientId, out var machineId))
        {
            EmitStatus(connection.ClientId, machineId, "Offline");
        }

        TraceEmitted?.Invoke(new NetworkTraceEntry("DISCONNECTED", connection.ClientId, string.Empty));
    }

    private void EmitStatus(string clientId, string machineId, string status)
    {
        var payload = new StatusPayload
        {
            MachineId = machineId,
            MachineName = machineId,
            Status = status,
            IpAddress = _listener.LocalEndpoint is IPEndPoint endpoint
                ? endpoint.Address.ToString()
                : string.Empty,
            LastSeen = DateTime.UtcNow
        };

        var packet = PacketFactory.CreateStatus(
            source: NetworkProtocol.ServerSource,
            target: machineId,
            payload: payload);

        TraceEmitted?.Invoke(new NetworkTraceEntry(
            "STATUS",
            clientId,
            NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(packet))));

        NotifyStatusEmitted(clientId, payload);
    }

    private void NotifyStatusEmitted(string clientId, StatusPayload payload)
    {
        try
        {
            StatusEmitted?.Invoke(payload);
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("STATUS_HANDLER_ERROR", clientId, ex.Message));
        }
    }

    public void Dispose()
    {
        _stopTokenSource.Cancel();

        foreach (ClientConnection connection in _connections.Values)
        {
            connection.Dispose();
        }

        _connections.Clear();
        _listener.Stop();
        _stopTokenSource.Dispose();
    }
}
