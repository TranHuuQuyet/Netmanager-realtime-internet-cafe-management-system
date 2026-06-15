using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.Enums;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Networking;
using ServerApp.Auth.Services;
namespace ServerApp.Networking;

public sealed record MachineCommandSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);

public sealed record MachineCommandAckResult(
    string MachineId,
    CommandType Command,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode,
    string RequestId);

public sealed class TcpJsonLineServer : IDisposable
{
    private sealed record PendingMachineCommand(
        string ClientId,
        string MachineId,
        PacketType PacketType,
        CommandType Command,
        string RequestId,
        DateTime CreatedUtc);

    private readonly TcpListener _listener;
    private readonly PacketDispatcher _dispatcher;
    private readonly ISessionService _sessions;
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly ConcurrentDictionary<string, string> _sessionBindings = new();
    private readonly ConcurrentDictionary<string, string> _machineBindings = new();
    private readonly ConcurrentDictionary<string, PendingMachineCommand> _pendingCommands = new(StringComparer.OrdinalIgnoreCase);
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

    public event Action<MachineCommandAckResult>? CommandResultEmitted;
// hàm gửi lệnh xuống máy client  trả về bool
    public async Task<bool> SendMachineCommandAsync(
        string machineId,// id máy 
        bool lockMachine,// lệnh khoá và mở khoá
        string issuedBy,// người ra lệnh
        string reason,// lý do ra lệnh
        CancellationToken cancellationToken = default) // token huỷ bỏ
    {//check id máy có tồn tại không
        string targetMachineId = machineId.Trim();// Trim xoá khoang trắng đầu cuối của id máy
        if (string.IsNullOrWhiteSpace(targetMachineId))
        {// gửi thông báo về lỗi id máy không hợp lệ
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ERROR", string.Empty, "Machine ID is required."));
            return false;
        }
// duyệt các _machineBindings 
        string clientId = string.Empty;
        foreach (KeyValuePair<string, string> binding in _machineBindings)
        {
            string boundClientId = binding.Key; // gán clientId bằng key của binding "tcp-0001"
            string boundMachineId = binding.Value;// gán boundMachineId bằng value của binding "machine-123"
// nếu boundMachineId trùng với targetMachineId thì gán clientId bằng boundClientId và thoát vòng lặp
            if (string.Equals(boundMachineId, targetMachineId, StringComparison.OrdinalIgnoreCase))//so sánh nhanh theo số nhị phân
            {
                clientId = boundClientId;// gán clientId bằng boundClientId "tcp-0001"
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(clientId)
            || !_connections.TryGetValue(clientId, out ClientConnection? connection))
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ERROR", targetMachineId, "Machine is not connected."));
            return false;
        }

        string requestId = Guid.NewGuid().ToString("N");
        PacketType packetType = lockMachine ? PacketType.LOCK : PacketType.UNLOCK;
        CommandType commandType = lockMachine ? CommandType.LOCK : CommandType.UNLOCK;
        Packet packet = lockMachine
            ? PacketFactory.CreateLock(
                source: NetworkProtocol.ServerSource,
                target: targetMachineId,
                payload: new LockPayload
                {
                    MachineId = targetMachineId,
                    IssuedBy = issuedBy,
                    Reason = reason
                },
                requestId: requestId)
            : PacketFactory.CreateUnlock(
                source: NetworkProtocol.ServerSource,
                target: targetMachineId,
                payload: new UnlockPayload
                {
                    MachineId = targetMachineId,
                    IssuedBy = issuedBy,
                    Reason = reason
                },
                requestId: requestId);

        string message = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(packet));
        TraceEmitted?.Invoke(new NetworkTraceEntry("OUT_COMMAND", clientId, message));
        await connection.SendAsync(message, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<MachineCommandSendResult> SendMachineCommandWithResultAsync(
        string machineId,
        bool lockMachine,
        string issuedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = machineId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetMachineId))
        {
            const string errorCode = "INVALID_MACHINE_ID";
            const string errorMessage = "Machine ID is required.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ERROR", string.Empty, $"{errorCode}: {errorMessage}"));
            return new MachineCommandSendResult(false, "Error", errorMessage, errorCode);
        }

        string clientId = string.Empty;
        foreach (KeyValuePair<string, string> binding in _machineBindings)
        {
            if (string.Equals(binding.Value, targetMachineId, StringComparison.OrdinalIgnoreCase))
            {
                clientId = binding.Key;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(clientId)
            || !_connections.TryGetValue(clientId, out ClientConnection? connection))
        {
            const string errorCode = "MACHINE_OFFLINE";
            const string errorMessage = "Machine is offline or not connected.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineCommandSendResult(false, "Error", errorMessage, errorCode);
        }

        string requestId = Guid.NewGuid().ToString("N");
        PacketType packetType = lockMachine ? PacketType.LOCK : PacketType.UNLOCK;
        CommandType commandType = lockMachine ? CommandType.LOCK : CommandType.UNLOCK;
        Packet packet = lockMachine
            ? PacketFactory.CreateLock(
                source: NetworkProtocol.ServerSource,
                target: targetMachineId,
                payload: new LockPayload
                {
                    MachineId = targetMachineId,
                    IssuedBy = issuedBy,
                    Reason = reason
                },
                requestId: requestId)
            : PacketFactory.CreateUnlock(
                source: NetworkProtocol.ServerSource,
                target: targetMachineId,
                payload: new UnlockPayload
                {
                    MachineId = targetMachineId,
                    IssuedBy = issuedBy,
                    Reason = reason
                },
                requestId: requestId);

        string message = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(packet));
        TraceEmitted?.Invoke(new NetworkTraceEntry("OUT_COMMAND", clientId, message));
        _pendingCommands[requestId] = new PendingMachineCommand(
            clientId,
            targetMachineId,
            packetType,
            commandType,
            requestId,
            DateTime.UtcNow);

        try
        {
            await connection.SendAsync(message, cancellationToken).ConfigureAwait(false);
            return new MachineCommandSendResult(true, "Submitted", "Command sent to client.", RequestId: requestId);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            const string errorCode = "COMMAND_SEND_FAILED";
            _pendingCommands.TryRemove(requestId, out _);
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ERROR", targetMachineId, $"{errorCode}: {ex.Message}"));
            return new MachineCommandSendResult(false, "Error", ex.Message, errorCode, requestId);
        }
    }

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

            if (result.BindSessionId is not null)
            {
                if (!TryBindSession(connection.ClientId, result.BindSessionId))
                {
                    await _sessions.CloseSessionAsync(result.BindSessionId, cancellationToken).ConfigureAwait(false);
                    connection.Disconnect();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(result.MachineId)
                && !string.IsNullOrWhiteSpace(result.MachineStatus))
            {
                string machineId = result.MachineId.Trim();
                _machineBindings[connection.ClientId] = machineId;
                EmitStatus(connection.ClientId, machineId, result.MachineStatus.Trim());
            }

            if (result.RequiresMachineBinding
                && !IsMachineBoundToClient(connection.ClientId, result.MachineId))
            {
                EmitCommandAckError(
                    connection.ClientId,
                    result.MachineId ?? string.Empty,
                    CommandType.LOCK,
                    "UNAUTHORIZED_COMMAND",
                    "ACK machine does not match authenticated connection.",
                    result.CommandAckPacket?.RequestId);
                return;
            }

            if (result.CommandAckPacket is not null)
            {
                HandleCommandAck(connection.ClientId, result.CommandAckPacket);
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.TraceDirection))
            {
                TraceEmitted?.Invoke(new NetworkTraceEntry(
                    result.TraceDirection,
                    connection.ClientId,
                    result.TraceMessage ?? string.Empty));
            }

            if (!string.IsNullOrWhiteSpace(result.Response))
            {
                TraceEmitted?.Invoke(new NetworkTraceEntry("OUT", connection.ClientId, result.Response));
                await connection.SendAsync(result.Response, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or JsonException)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("DISPATCH_ERROR", connection.ClientId, ex.Message));
            connection.Disconnect();
        }
    }

    private bool TryBindSession(string clientId, string sessionId)
    {
        if (_sessionBindings.TryGetValue(clientId, out string? existingSessionId))
        {
            return string.Equals(existingSessionId, sessionId, StringComparison.OrdinalIgnoreCase);
        }

        return _sessionBindings.TryAdd(clientId, sessionId);
    }

    private bool IsMachineBoundToClient(string clientId, string? machineId)
    {
        return !string.IsNullOrWhiteSpace(machineId)
            && _machineBindings.TryGetValue(clientId, out string? boundMachineId)
            && string.Equals(boundMachineId, machineId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void HandleCommandAck(string clientId, Packet<AckPayload> ackPacket)
    {
        AckPayload payload = ackPacket.TypedPayload;
        string requestId = ackPacket.RequestId ?? string.Empty;
        string machineId = payload.MachineId.Trim();

        if (!_pendingCommands.TryGetValue(requestId, out PendingMachineCommand? pendingCommand))
        {
            EmitCommandAckError(
                clientId,
                machineId,
                ParseAckCommand(payload.AckFor),
                "ACK_UNKNOWN_REQUEST",
                "ACK requestId does not match a pending command.",
                requestId);
            return;
        }

        if (!string.Equals(pendingCommand.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
        {
            EmitCommandAckError(
                clientId,
                machineId,
                pendingCommand.Command,
                "UNAUTHORIZED_COMMAND",
                "ACK came from a different authenticated connection.",
                requestId);
            return;
        }

        if (!string.Equals(pendingCommand.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
        {
            EmitCommandAckError(
                clientId,
                machineId,
                pendingCommand.Command,
                "ACK_MACHINE_MISMATCH",
                "ACK machineId does not match the pending command.",
                requestId);
            return;
        }

        if (!string.Equals(pendingCommand.PacketType.ToString(), payload.AckFor, StringComparison.OrdinalIgnoreCase))
        {
            EmitCommandAckError(
                clientId,
                machineId,
                pendingCommand.Command,
                "ACK_TYPE_MISMATCH",
                "ACK type does not match the pending command.",
                requestId);
            return;
        }

        _pendingCommands.TryRemove(requestId, out _);

        string status = payload.Status.Trim();
        bool isSuccess = string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase);
        var result = new MachineCommandAckResult(
            machineId,
            pendingCommand.Command,
            status,
            string.IsNullOrWhiteSpace(payload.Message) ? "Command ACK received." : payload.Message!,
            IsError: !isSuccess,
            ErrorCode: isSuccess ? null : $"COMMAND_ACK_{status.ToUpperInvariant()}",
            requestId);

        TraceEmitted?.Invoke(new NetworkTraceEntry(
            isSuccess ? "COMMAND_ACK" : "COMMAND_ACK_ERROR",
            clientId,
            NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(ackPacket))));
        NotifyCommandResultEmitted(clientId, result);
    }

    private void EmitCommandAckError(
        string clientId,
        string machineId,
        CommandType command,
        string errorCode,
        string message,
        string? requestId)
    {
        TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ACK_ERROR", clientId, $"{errorCode}: {message}"));

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            NotifyCommandResultEmitted(
                clientId,
                new MachineCommandAckResult(
                    string.IsNullOrWhiteSpace(machineId) ? "UNKNOWN" : machineId,
                    command,
                    "Error",
                    message,
                    IsError: true,
                    errorCode,
                    requestId));
        }
    }

    private static CommandType ParseAckCommand(string? ackFor)
        => string.Equals(ackFor, PacketType.UNLOCK.ToString(), StringComparison.OrdinalIgnoreCase)
            ? CommandType.UNLOCK
            : CommandType.LOCK;

    private void NotifyCommandResultEmitted(string clientId, MachineCommandAckResult result)
    {
        try
        {
            CommandResultEmitted?.Invoke(result);
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_RESULT_HANDLER_ERROR", clientId, ex.Message));
        }
    }

    private void ClientDisconnected(ClientConnection connection)
    {
        _connections.TryRemove(connection.ClientId, out _);
        CleanupClientState(connection.ClientId);
        TraceEmitted?.Invoke(new NetworkTraceEntry("DISCONNECTED", connection.ClientId, string.Empty));
    }

    private void CleanupClientState(string clientId)
    {
        if (_sessionBindings.TryRemove(clientId, out var sessionId))
        {
            CloseBoundSession(clientId, sessionId);
        }

        if (_machineBindings.TryRemove(clientId, out var machineId))
        {
            EmitStatus(clientId, machineId, "Offline");
        }

        foreach (KeyValuePair<string, PendingMachineCommand> pendingCommand in _pendingCommands.ToArray())
        {
            if (string.Equals(pendingCommand.Value.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
            {
                _pendingCommands.TryRemove(pendingCommand.Key, out _);
            }
        }
    }

    private void CloseBoundSession(string clientId, string sessionId)
    {
        try
        {
            _sessions.CloseSessionAsync(sessionId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("SESSION_ERROR", clientId, ex.Message));
        }
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

        foreach (ClientConnection connection in _connections.Values.ToArray())
        {
            connection.MessageReceived -= ClientMessageReceived;
            connection.Disconnected -= ClientDisconnected;
            _connections.TryRemove(connection.ClientId, out _);
            CleanupClientState(connection.ClientId);
            TraceEmitted?.Invoke(new NetworkTraceEntry("DISCONNECTED", connection.ClientId, string.Empty));
            connection.Dispose();
        }

        _connections.Clear();
        _sessionBindings.Clear();
        _machineBindings.Clear();
        _listener.Stop();
        _stopTokenSource.Dispose();
    }
}
