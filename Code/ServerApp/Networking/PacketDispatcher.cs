using System.IO;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp.Networking;

public sealed record PacketDispatchResult(string Response, string? OpenedSessionId = null, string? MachineId = null);

public sealed class PacketDispatcher
{
    private readonly IAuthService _authService;

    public PacketDispatcher(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<PacketDispatchResult> DispatchAsync(string inboundLine, CancellationToken cancellationToken = default)
    {
        object packet = JsonHelper.DeserializePacket(inboundLine);

        return packet switch
        {
            Packet<LoginPayload> loginPacket => await DispatchLoginAsync(loginPacket, cancellationToken).ConfigureAwait(false),
            Packet typedPacket => throw new InvalidDataException($"Unsupported packet type: {typedPacket.Type}"),
            _ => throw new InvalidDataException("Unsupported packet envelope.")
        };
    }

    private async Task<PacketDispatchResult> DispatchLoginAsync(
        Packet<LoginPayload> loginPacket,
        CancellationToken cancellationToken)
    {
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

        return new PacketDispatchResult(SerializeResponse(response), result.Session.Id, result.User.MachineId);
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

    private static string SerializeResponse(Packet response)
    {
        return NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(response));
    }
}
