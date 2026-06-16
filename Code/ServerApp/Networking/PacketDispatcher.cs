using System.IO;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Enums;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;

namespace ServerApp.Networking;

// Kết quả sau khi PacketDispatcher đọc một JSON-line từ client.
//
// Dispatcher không trực tiếp giữ TCP connection.
// Nó chỉ phân loại packet và trả metadata để TcpJsonLineServer xử lý tiếp.
//
// Các nhánh chính:
// - Response: packet server cần gửi lại client, ví dụ LOGIN response hoặc STATUS ACK.
// - BindSessionId/MachineId/MachineStatus: thông tin để server bind session/machine vào connection.
// - CommandAckPacket: ACK hợp lệ về mặt payload, cần server check binding + pending command.
// - CommandError*: ACK sai format, cần server emit typed result cho UI.
public sealed record PacketDispatchResult(
    string? Response,
    string? BindSessionId = null,
    string? MachineId = null,
    string? MachineStatus = null,
    bool RequiresMachineBinding = false,
    Packet<AckPayload>? CommandAckPacket = null,
    // ACK validation errors are carried as structured fields so the server can emit typed UI results.
    string? CommandErrorCode = null,
    string? CommandErrorMessage = null,
    CommandType? CommandErrorCommand = null,
    string? CommandErrorRequestId = null,
    string? TraceDirection = null,
    string? TraceMessage = null);

// Chịu trách nhiệm parse packet và validate payload ở tầng protocol/business nhẹ.
//
// Flow:
// - LOGIN: gọi AuthService, tạo LOGIN success/failure response.
// - STATUS: validate session/machine, update machine status, trả ACK.
// - ACK: validate hình dạng ACK command, chưa quyết định ACK có khớp pending command hay không.
//
// Không làm ở đây:
// - Không gửi socket.
// - Không biết clientId TCP.
// - Không biết pending command đang nằm trong server nào.
public sealed class PacketDispatcher
{
    private readonly IAuthService _authService;
    private readonly ISessionRepository _sessions;
    private readonly IMachineRepository _machines;

    public PacketDispatcher(
        IAuthService authService,
        ISessionRepository sessions,
        IMachineRepository machines)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _machines = machines ?? throw new ArgumentNullException(nameof(machines));
    }

    public async Task<PacketDispatchResult> DispatchAsync(string inboundLine, CancellationToken cancellationToken = default)
    {
        // Điểm vào chung cho mọi JSON-line client gửi lên server.
        //
        // Thường lỗi khi:
        // - JSON sai format
        // - type không thuộc enum PacketType
        // - payload không đúng schema packet tương ứng
        object packet = JsonHelper.DeserializePacket(inboundLine);

        return packet switch
        {
            Packet<LoginPayload> loginPacket => await DispatchLoginAsync(loginPacket, cancellationToken).ConfigureAwait(false),
            Packet<StatusPayload> statusPacket => await DispatchStatusAsync(statusPacket, cancellationToken).ConfigureAwait(false),
            Packet<AckPayload> ackPacket => DispatchAck(ackPacket),
            Packet typedPacket => throw new InvalidDataException($"Unsupported packet type: {typedPacket.Type}"),
            _ => throw new InvalidDataException("Unsupported packet envelope.")
        };
    }

    private async Task<PacketDispatchResult> DispatchLoginAsync(
        Packet<LoginPayload> loginPacket,
        CancellationToken cancellationToken)
    {
        // LOGIN flow:
        // - kiểm tra payload/requestId/role
        // - gọi AuthService để xác thực user
        // - nếu thành công, trả sessionId và machineId cho client
        // - nếu thất bại, trả LOGIN failure có error code deterministic
        LoginPayload? payload = loginPacket.TypedPayload;

        if (payload is null)
        {
            return CreateLoginFailure(loginPacket, "INVALID_PACKET", "LOGIN payload is required.");
        }

        if (string.IsNullOrWhiteSpace(loginPacket.RequestId))
        {
            return CreateLoginFailure(loginPacket, "INVALID_PACKET", "LOGIN requestId is required.");
        }

        if (!Enum.TryParse(payload.Role, ignoreCase: true, out UserRole requiredRole)
            || !Enum.IsDefined(requiredRole)
            || int.TryParse(payload.Role, out _))
        {
            return CreateLoginFailure(loginPacket, "INVALID_PACKET", "LOGIN role must be Admin or Client.");
        }

        AuthResult result = await _authService.AuthenticateAsync(
            new AuthRequest(payload.Username, payload.Password, payload.MachineId, requiredRole),
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return CreateLoginFailure(
                loginPacket,
                result.ErrorCode ?? "SERVER_ERROR",
                result.Message);
        }

        if (result.User is null || result.Session is null)
        {
            return CreateLoginFailure(loginPacket, "SERVER_ERROR", "Authentication result is incomplete.");
        }

        var response = PacketFactory.CreateLoginSuccess(
            source: NetworkProtocol.ServerSource,
            target: loginPacket.Source,
            payload: new LoginResultPayload
            {
                SessionId = result.Session.Id,
                Username = result.User.Username,
                Role = result.User.Role.ToString(),
                MachineId = result.User.MachineId
            },
            requestId: loginPacket.RequestId,
            message: result.Message);

        return new PacketDispatchResult(
            SerializeResponse(response),
            BindSessionId: result.Session.Id,
            MachineId: result.User.MachineId,
            MachineStatus: "Online");
    }

    private async Task<PacketDispatchResult> DispatchStatusAsync(
        Packet<StatusPayload> statusPacket,
        CancellationToken cancellationToken)
    {
        // STATUS flow:
        // - client báo trạng thái máy hiện tại
        // - nếu có sessionId thì session phải tồn tại, chưa revoke, và khớp machineId
        // - server update repository rồi trả ACK Accepted/Rejected
        StatusPayload? payload = statusPacket.TypedPayload;
        if (payload is null)
        {
            return CreateStatusAck(statusPacket, string.Empty, "Rejected", "STATUS payload is required.");
        }

        string machineId = payload.MachineId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return CreateStatusAck(statusPacket, string.Empty, "Rejected", "STATUS machineId is required.");
        }

        string? sessionId = string.IsNullOrWhiteSpace(payload.SessionId)
            ? null
            : payload.SessionId.Trim();

        if (sessionId is not null)
        {
            SessionRecord? session = await _sessions.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (session is null
                || session.State == SessionState.Revoked
                || !string.Equals(session.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                return CreateStatusAck(statusPacket, machineId, "Rejected", "STATUS session is not valid for this machine.");
            }
        }

        string status = string.IsNullOrWhiteSpace(payload.Status)
            ? "Online"
            : payload.Status.Trim();
        DateTime lastSeen = payload.LastSeen == default
            ? DateTime.UtcNow
            : payload.LastSeen.ToUniversalTime();

        await _machines.UpdateStatusAsync(machineId, status, lastSeen, cancellationToken).ConfigureAwait(false);

        PacketDispatchResult ack = CreateStatusAck(statusPacket, machineId, "Accepted", "STATUS accepted.");
        return ack with
        {
            BindSessionId = sessionId,
            MachineId = machineId,
            MachineStatus = status
        };
    }

    private static PacketDispatchResult CreateLoginFailure(
        Packet<LoginPayload> loginPacket,
        string errorCode,
        string details)
    {
        var response = PacketFactory.CreateLoginFailed(
            source: NetworkProtocol.ServerSource,
            target: loginPacket.Source,
            errorCode: errorCode,
            details: details,
            requestId: loginPacket.RequestId);

        return new PacketDispatchResult(SerializeResponse(response));
    }

    private static PacketDispatchResult CreateStatusAck(
        Packet<StatusPayload> statusPacket,
        string machineId,
        string status,
        string message)
    {
        var response = PacketFactory.CreateAck(
            source: NetworkProtocol.ServerSource,
            target: statusPacket.Source,
            payload: new AckPayload
            {
                MachineId = machineId,
                AckFor = statusPacket.Type.ToString(),
                Status = status,
                Message = message
            },
            requestId: statusPacket.RequestId);

        return new PacketDispatchResult(SerializeResponse(response));
    }

    private static PacketDispatchResult DispatchAck(Packet<AckPayload> ackPacket)
    {
        // ACK flow ở tầng dispatcher:
        // - chỉ kiểm tra ACK có đủ machineId/requestId/ackFor/status hay không
        // - không check client này có đúng là máy đó không
        // - không check requestId có nằm trong pending command không
        //
        // Những bước còn lại cần clientId TCP nên được làm trong TcpJsonLineServer.
        AckPayload? payload = ackPacket.TypedPayload;
        if (payload is null)
        {
            return CreateCommandAckError(
                "UNKNOWN",
                CommandType.LOCK,
                "INVALID_PACKET",
                "ACK payload is required.",
                ackPacket.RequestId);
        }

        string machineId = payload.MachineId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return CreateCommandAckError(
                string.Empty,
                ParseCommandOrDefault(payload.AckFor),
                "INVALID_MACHINE_ID",
                "ACK machineId is required.",
                ackPacket.RequestId);
        }

        if (string.IsNullOrWhiteSpace(ackPacket.RequestId))
        {
            return CreateCommandAckError(
                machineId,
                ParseCommandOrDefault(payload.AckFor),
                "ACK_REQUEST_ID_REQUIRED",
                "ACK requestId is required.",
                ackPacket.RequestId);
        }

        string ackFor = payload.AckFor?.Trim() ?? string.Empty;
        if (!Enum.TryParse(ackFor, ignoreCase: true, out PacketType ackForType)
            || ackForType is not (PacketType.LOCK or PacketType.UNLOCK))
        {
            return CreateCommandAckError(
                machineId,
                ParseCommandOrDefault(payload.AckFor),
                "ACK_TYPE_MISMATCH",
                "ACK must reference LOCK or UNLOCK.",
                ackPacket.RequestId);
        }

        string status = payload.Status?.Trim() ?? string.Empty;
        if (!string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Ignored", StringComparison.OrdinalIgnoreCase))
        {
            return CreateCommandAckError(
                machineId,
                ToCommandType(ackForType),
                "INVALID_PACKET",
                "ACK status must be Success, Failed or Ignored.",
                ackPacket.RequestId);
        }

        string message = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(ackPacket));
        return new PacketDispatchResult(
            Response: null,
            MachineId: machineId,
            RequiresMachineBinding: true,
            CommandAckPacket: ackPacket,
            TraceDirection: "COMMAND_ACK",
            TraceMessage: message);
    }

    private static PacketDispatchResult CreateCommandAckError(
        string machineId,
        CommandType command,
        string errorCode,
        string message,
        string? requestId)
        => new(
            Response: null,
            MachineId: machineId,
            CommandErrorCode: errorCode,
            CommandErrorMessage: message,
            CommandErrorCommand: command,
            CommandErrorRequestId: requestId ?? string.Empty,
            TraceDirection: "COMMAND_ACK_ERROR",
            TraceMessage: $"{errorCode}: {message}");

    private static CommandType ParseCommandOrDefault(string? ackFor)
        => Enum.TryParse(ackFor?.Trim(), ignoreCase: true, out PacketType packetType)
            ? ToCommandType(packetType)
            : CommandType.LOCK;

    private static CommandType ToCommandType(PacketType packetType)
        => packetType == PacketType.UNLOCK
            ? CommandType.UNLOCK
            : CommandType.LOCK;

    private static string SerializeResponse(Packet response)
    {
        return NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(response));
    }
}
