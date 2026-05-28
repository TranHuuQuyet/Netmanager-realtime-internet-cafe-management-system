using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ServerApp.Networking;

public sealed class TcpJsonLineServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly PacketDispatcher _dispatcher;
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private Task? _acceptLoopTask;
    private int _nextClientNumber;
    private bool _isStarted;

    public TcpJsonLineServer(IPAddress address, int port, PacketDispatcher? dispatcher = null)
    {
        _listener = new TcpListener(address, port);
        _dispatcher = dispatcher ?? new PacketDispatcher();
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
            string response = _dispatcher.Dispatch(message);
            TraceEmitted?.Invoke(new NetworkTraceEntry("OUT", connection.ClientId, response));
            await connection.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("DISPATCH_ERROR", connection.ClientId, ex.Message));
        }
    }

    private void ClientDisconnected(ClientConnection connection)
    {
        _connections.TryRemove(connection.ClientId, out _);
        TraceEmitted?.Invoke(new NetworkTraceEntry("DISCONNECTED", connection.ClientId, string.Empty));
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
