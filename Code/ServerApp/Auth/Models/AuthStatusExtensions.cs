using Shared.DTOs.ResponsePayloads;
using Shared.Models;

namespace ServerApp.Auth.Models;

public static class AuthStatusExtensions
{
    // Map trang thai noi bo sang ma loi API de networking/UI co the hien thi dong nhat.
    public static string ToApiErrorCode(this AuthStatus status) => status switch
    {
        AuthStatus.Success => string.Empty,
        AuthStatus.InvalidInput => "INVALID_PACKET",
        AuthStatus.InvalidCredentials => "INVALID_CREDENTIALS",
        AuthStatus.InvalidMachineId => "INVALID_MACHINE_ID",
        AuthStatus.AccountMachineMismatch => "ACCOUNT_MACHINE_MISMATCH",
        AuthStatus.AccountDisabled => "ACCOUNT_DISABLED",
        AuthStatus.RoleMismatch => "INVALID_CREDENTIALS",
        AuthStatus.ServerError => "SERVER_ERROR",
        _ => "SERVER_ERROR"
    };

    // Gom envelope top-level cho dispatcher/UI de khong phai tu map status va message.
    public static ErrorInfo? ToErrorInfo(this AuthResult result)
    {
        if (result.IsSuccess)
        {
            return null;
        }

        return new ErrorInfo
        {
            Code = result.Status.ToApiErrorCode(),
            Details = result.Message
        };
    }

    // Gom payload fail LOGIN theo dung contract shared, giu ui khong can doc status noi bo.
    public static EmptyPayload ToLoginFailedPayload(this AuthResult result, DateTime? issuedAtUtc = null)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful auth result cannot be mapped to a failed login payload.");
        }

        return new EmptyPayload();
    }
}
