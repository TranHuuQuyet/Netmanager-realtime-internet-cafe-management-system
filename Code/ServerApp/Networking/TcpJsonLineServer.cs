using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Shared.DTOs.Bidrectional;
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

// TCP JSON-line server của ServerApp.
//
// Vai trò chính:
// - Lắng nghe client kết nối TCP.
// - Nhận từng dòng JSON, đưa qua PacketDispatcher.
// - Bind session/machine vào đúng TCP connection sau LOGIN/STATUS.
// - Gửi LOCK/UNLOCK từ admin UI xuống đúng máy client.
// - Theo dõi pending command và biến ACK thành typed result cho UI.
//
// Một command admin hoàn chỉnh đi qua flow:
// Admin click -> SendMachineCommandWithResultAsync -> gửi packet -> lưu pending requestId
// -> client ACK -> HandleCommandAck -> CommandResultEmitted -> MainForm hiển thị kết quả.
public sealed class TcpJsonLineServer : IDisposable
{
    private static readonly TimeSpan PendingCommandTimeout = TimeSpan.FromSeconds(30);

    // Một command đã gửi xuống client nhưng chưa nhận ACK cuối.
    //
    // Cần lưu đủ thông tin để validate ACK:
    // - ACK phải đến từ đúng TCP clientId.
    // - ACK phải đúng machineId.
    // - ACK phải đúng requestId.
    // - ACK phải đúng loại command LOCK/UNLOCK.
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
        // Start server đúng một lần.
        //
        // Sau khi listener.Start(), AcceptLoopAsync chạy nền để nhận client mới.
        // Không await ở đây vì UI vẫn cần tiếp tục chạy.
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

    public event Action<string, ChatPayload>? ChatReceived;

    public event Action<string, TimerPayload>? TimerSent;

    // Event đưa kết quả command cuối cùng sang tầng UI.
    //
    // Có thể được emit khi:
    // - client ACK success
    // - client ACK Failed/Ignored
    // - ACK sai requestId/type/machine
    // - client disconnect trước ACK
    // - pending command timeout
    public event Action<MachineCommandAckResult>? CommandResultEmitted;

    [Obsolete("Use SendMachineCommandWithResultAsync to preserve requestId, pending tracking and deterministic command results.")]
    public async Task<bool> SendMachineCommandAsync(
        string machineId,
        bool lockMachine,
        string issuedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        // API cũ chỉ trả bool.
        //
        // Giữ lại để code cũ không gãy, nhưng toàn bộ flow thật được chuyển sang
        // SendMachineCommandWithResultAsync để vẫn có requestId, pending tracking và error code.
        MachineCommandSendResult result = await SendMachineCommandWithResultAsync(
            machineId,
            lockMachine,
            issuedBy,
            reason,
            cancellationToken).ConfigureAwait(false);

        return result.Sent;
    }
// hàm gửi lệnh xuống máy client  trả về bool
    [Obsolete("Legacy command path retained only for local reference; use SendMachineCommandWithResultAsync.")]
    private async Task<bool> SendMachineCommandLegacyAsync(
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
        // Submit command flow:
        // - validate machineId
        // - tìm TCP client đang bind với machineId
        // - gọi authorization guard để chắc machine có active session hợp lệ
        // - tạo packet LOCK/UNLOCK có requestId
        // - gửi xuống socket
        // - lưu pending command để ACK sau này có thể đối chiếu
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

        AuthResult authorization = await _sessions.AuthorizeCommandTargetAsync(targetMachineId, cancellationToken).ConfigureAwait(false);
        if (!authorization.IsSuccess)
        {
            const string errorCode = "UNAUTHORIZED_COMMAND";
            string errorMessage = authorization.Message;
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
            _ = ExpirePendingCommandAsync(requestId, PendingCommandTimeout, _stopTokenSource.Token);
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

    public async Task<MachineChatSendResult> SendChatAsync(
        string machineId,
        string sender,
        string message,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = machineId?.Trim() ?? string.Empty;
        string chatMessage = message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(targetMachineId))
        {
            const string errorCode = "INVALID_MACHINE_ID";
            const string errorMessage = "Machine ID is required.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_ERROR", string.Empty, $"{errorCode}: {errorMessage}"));
            return new MachineChatSendResult(false, "Error", errorMessage, errorCode);
        }

        if (string.IsNullOrWhiteSpace(chatMessage))
        {
            const string errorCode = "INVALID_CHAT_MESSAGE";
            const string errorMessage = "CHAT message is required.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineChatSendResult(false, "Error", errorMessage, errorCode);
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
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineChatSendResult(false, "Error", errorMessage, errorCode);
        }

        AuthResult authorization = await _sessions.AuthorizeCommandTargetAsync(targetMachineId, cancellationToken).ConfigureAwait(false);
        if (!authorization.IsSuccess)
        {
            const string errorCode = "UNAUTHORIZED_CHAT";
            string errorMessage = authorization.Message;
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineChatSendResult(false, "Error", errorMessage, errorCode);
        }

        string requestId = Guid.NewGuid().ToString("N");
        Packet<ChatPayload> packet = PacketFactory.CreateChat(
            source: NetworkProtocol.ServerSource,
            target: targetMachineId,
            payload: new ChatPayload
            {
                Sender = string.IsNullOrWhiteSpace(sender) ? "Admin" : sender.Trim(),
                Receiver = targetMachineId,
                Message = chatMessage
            },
            requestId: requestId);

        string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(packet));
        TraceEmitted?.Invoke(new NetworkTraceEntry("OUT_CHAT", clientId, outboundLine));

        try
        {
            await connection.SendAsync(outboundLine, cancellationToken).ConfigureAwait(false);
            return new MachineChatSendResult(true, "Sent", "CHAT sent to client.", RequestId: requestId);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            const string errorCode = "CHAT_SEND_FAILED";
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_ERROR", targetMachineId, $"{errorCode}: {ex.Message}"));
            return new MachineChatSendResult(false, "Error", ex.Message, errorCode, requestId);
        }
    }

    public async Task<MachineTimerSendResult> SendTimerAsync(
        string machineId,
        TimerPayload payload,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = machineId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetMachineId))
        {
            const string errorCode = "INVALID_MACHINE_ID";
            const string errorMessage = "Machine ID is required.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("TIMER_ERROR", string.Empty, $"{errorCode}: {errorMessage}"));
            return new MachineTimerSendResult(false, "Error", errorMessage, errorCode);
        }

        if (payload is null)
        {
            const string errorCode = "INVALID_TIMER_PAYLOAD";
            const string errorMessage = "TIMER payload is required.";
            TraceEmitted?.Invoke(new NetworkTraceEntry("TIMER_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineTimerSendResult(false, "Error", errorMessage, errorCode);
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
            TraceEmitted?.Invoke(new NetworkTraceEntry("TIMER_ERROR", targetMachineId, $"{errorCode}: {errorMessage}"));
            return new MachineTimerSendResult(false, "Error", errorMessage, errorCode);
        }

        string requestId = Guid.NewGuid().ToString("N");
        payload.MachineId = targetMachineId;
        Packet<TimerPayload> packet = PacketFactory.CreateTimer(
            source: NetworkProtocol.ServerSource,
            target: targetMachineId,
            payload: payload,
            requestId: requestId);

        string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(packet));
        TraceEmitted?.Invoke(new NetworkTraceEntry("OUT_TIMER", clientId, outboundLine));

        try
        {
            await connection.SendAsync(outboundLine, cancellationToken).ConfigureAwait(false);
            NotifyTimerSent(clientId, targetMachineId, payload);
            return new MachineTimerSendResult(true, "Sent", "TIMER sent to client.", RequestId: requestId);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            const string errorCode = "TIMER_SEND_FAILED";
            TraceEmitted?.Invoke(new NetworkTraceEntry("TIMER_ERROR", targetMachineId, $"{errorCode}: {ex.Message}"));
            return new MachineTimerSendResult(false, "Error", ex.Message, errorCode, requestId);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        // Vòng lặp nhận client TCP mới.
        //
        // Mỗi client được gán clientId dạng tcp-0001, tcp-0002...
        // Sau đó server đăng ký event MessageReceived/Disconnected
        // và để ClientConnection tự chạy ReceiveLoopAsync.
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
            // Server đang stop/dispose nên vòng accept bị hủy bình thường.
        }
        catch (ObjectDisposedException)
        {
            // Listener đã bị dispose trong lúc AcceptTcpClientAsync đang chờ.
        }
        catch (SocketException ex)
        {
            // Lỗi socket ở tầng listener, ví dụ port bị đóng hoặc network stack lỗi.
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
        // Receive message flow:
        // - trace raw JSON client gửi lên
        // - dispatcher parse và validate packet
        // - nếu LOGIN/STATUS thành công thì bind session/machine với connection
        // - nếu ACK lỗi format thì emit typed result
        // - nếu ACK hợp lệ format thì check binding/pending command
        // - nếu có response thì gửi lại client
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

            if (!string.IsNullOrWhiteSpace(result.CommandErrorCode))
            {
                EmitCommandAckError(
                    connection.ClientId,
                    result.MachineId ?? string.Empty,
                    result.CommandErrorCommand ?? CommandType.LOCK,
                    result.CommandErrorCode,
                    result.CommandErrorMessage ?? result.CommandErrorCode,
                    result.CommandErrorRequestId);
                return;
            }

            if (result.RequiresMachineBinding
                && !IsMachineBoundToClient(connection.ClientId, result.MachineId))
            {
                EmitCommandAckError(
                    connection.ClientId,
                    result.MachineId ?? string.Empty,
                    ParseAckCommand(result.CommandAckPacket?.TypedPayload.AckFor),
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

            if (result.ChatPacket is not null)
            {
                HandleChatPacket(connection.ClientId, result.ChatPacket);
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
            // Lỗi packet/stream ở client hiện tại.
            //
            // Thường xảy ra khi:
            // - client gửi JSON sai format
            // - packet type không deserialize được
            // - payload thiếu/sai schema nặng
            // - stream lỗi trong lúc xử lý
            TraceEmitted?.Invoke(new NetworkTraceEntry("DISPATCH_ERROR", connection.ClientId, ex.Message));
            connection.Disconnect();
        }
    }

    private bool TryBindSession(string clientId, string sessionId)
    {
        // Mỗi TCP connection chỉ được bind một sessionId.
        //
        // Nếu cùng connection cố đổi sessionId, server đóng connection để tránh
        // một socket giả danh nhiều phiên đăng nhập khác nhau.
        if (_sessionBindings.TryGetValue(clientId, out string? existingSessionId))
        {
            return string.Equals(existingSessionId, sessionId, StringComparison.OrdinalIgnoreCase);
        }

        return _sessionBindings.TryAdd(clientId, sessionId);
    }

    private bool IsMachineBoundToClient(string clientId, string? machineId)
    {
        // ACK command phải đến từ đúng connection đã bind với machineId đó.
        //
        // Chặn trường hợp client A gửi ACK thay cho máy B.
        return !string.IsNullOrWhiteSpace(machineId)
            && _machineBindings.TryGetValue(clientId, out string? boundMachineId)
            && string.Equals(boundMachineId, machineId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void HandleCommandAck(string clientId, Packet<AckPayload> ackPacket)
    {
        // ACK validation flow:
        // - requestId phải tồn tại trong _pendingCommands
        // - ACK phải đến từ đúng TCP client đã nhận command
        // - machineId trong ACK phải khớp command đang pending
        // - ackFor phải khớp LOCK/UNLOCK đã gửi
        //
        // Nếu một bước sai, server emit typed error result cho admin UI.
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
            ErrorCode: isSuccess ? null : GetAckErrorCode(status),
            requestId);

        TraceEmitted?.Invoke(new NetworkTraceEntry(
            isSuccess ? "COMMAND_ACK" : "COMMAND_ACK_ERROR",
            clientId,
            NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(ackPacket))));
        NotifyCommandResultEmitted(clientId, result);
    }

    private void HandleChatPacket(string clientId, Packet<ChatPayload> chatPacket)
    {
        ChatPayload payload = chatPacket.TypedPayload;
        string senderMachineId = payload.Sender.Trim();

        if (!IsMachineBoundToClient(clientId, senderMachineId))
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry(
                "CHAT_ERROR",
                clientId,
                "UNAUTHORIZED_CHAT: CHAT sender does not match authenticated connection."));
            return;
        }

        TraceEmitted?.Invoke(new NetworkTraceEntry(
            "CHAT",
            clientId,
            NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(chatPacket))));

        NotifyChatReceived(clientId, senderMachineId, payload);
    }

    private void EmitCommandAckError(
        string clientId,
        string machineId,
        CommandType command,
        string errorCode,
        string message,
        string? requestId)
    {
        // Tạo result lỗi dạng typed cho ACK/command.
        //
        // Khác với trace string thuần:
        // - UI/M3 đọc được machineId, command, errorCode, requestId rõ ràng.
        // - Test có thể assert deterministic error code.
        // - Admin thấy kết quả cuối thay vì chỉ có log COMMAND_ACK_ERROR.
        TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_ACK_ERROR", clientId, $"{errorCode}: {message}"));

        NotifyCommandResultEmitted(
            clientId,
            new MachineCommandAckResult(
                string.IsNullOrWhiteSpace(machineId) ? "UNKNOWN" : machineId,
                command,
                "Error",
                message,
                IsError: true,
                errorCode,
                requestId ?? string.Empty));
    }

    private static CommandType ParseAckCommand(string? ackFor)
        // ACK malformed vẫn cần command type để UI hiển thị.
        // Nếu không parse được thì fallback LOCK để giữ result không null.
        => string.Equals(ackFor, PacketType.UNLOCK.ToString(), StringComparison.OrdinalIgnoreCase)
            ? CommandType.UNLOCK
            : CommandType.LOCK;

    private static string GetAckErrorCode(string status)
        // Status hợp lệ nhưng không thành công được map thành fixed error code.
        //
        // Failed  -> COMMAND_ACK_FAILED
        // Ignored -> COMMAND_ACK_IGNORED
        => string.Equals(status, "Ignored", StringComparison.OrdinalIgnoreCase)
            ? "COMMAND_ACK_IGNORED"
            : "COMMAND_ACK_FAILED";

    private async Task ExpirePendingCommandAsync(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // Timeout flow:
        // - sau khi gửi command thành công, server chờ ACK trong PendingCommandTimeout
        // - nếu ACK đến trước, pending command đã bị remove nên timeout không làm gì
        // - nếu client im lặng, server remove pending và emit COMMAND_ACK_TIMEOUT
        try
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (_pendingCommands.TryRemove(requestId, out PendingMachineCommand? pendingCommand))
        {
            EmitPendingCommandError(
                pendingCommand,
                "COMMAND_ACK_TIMEOUT",
                "Client did not ACK command before timeout.");
        }
    }

    private void EmitPendingCommandError(
        PendingMachineCommand pendingCommand,
        string errorCode,
        string message)
    {
        // Dùng lại format lỗi ACK cho các lỗi lifecycle của pending command.
        //
        // Ví dụ:
        // - client disconnect trước ACK
        // - client không ACK trước timeout
        EmitCommandAckError(
            pendingCommand.ClientId,
            pendingCommand.MachineId,
            pendingCommand.Command,
            errorCode,
            message,
            pendingCommand.RequestId);
    }

    private void NotifyCommandResultEmitted(string clientId, MachineCommandAckResult result)
    {
        // Gọi event handler của UI/service boundary.
        //
        // Nếu UI handler lỗi, server chỉ trace lỗi đó,
        // không để một exception UI làm chết TCP server.
        try
        {
            CommandResultEmitted?.Invoke(result);
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("COMMAND_RESULT_HANDLER_ERROR", clientId, ex.Message));
        }
    }

    private void NotifyChatReceived(string clientId, string machineId, ChatPayload payload)
    {
        try
        {
            ChatReceived?.Invoke(machineId, payload);
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("CHAT_HANDLER_ERROR", clientId, ex.Message));
        }
    }

    private void NotifyTimerSent(string clientId, string machineId, TimerPayload payload)
    {
        try
        {
            TimerSent?.Invoke(machineId, payload);
        }
        catch (Exception ex)
        {
            TraceEmitted?.Invoke(new NetworkTraceEntry("TIMER_HANDLER_ERROR", clientId, ex.Message));
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
        // Cleanup khi client disconnect:
        // - đóng session đang bind
        // - emit máy Offline
        // - mọi command pending của client này nhận COMMAND_CLIENT_DISCONNECTED
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
                if (_pendingCommands.TryRemove(pendingCommand.Key, out PendingMachineCommand? removedCommand))
                {
                    EmitPendingCommandError(
                        removedCommand,
                        "COMMAND_CLIENT_DISCONNECTED",
                        "Client disconnected before ACK.");
                }
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
