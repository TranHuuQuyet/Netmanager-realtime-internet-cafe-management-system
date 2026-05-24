namespace ServerApp.Auth.Models;

public static class AuthStatusExtensions {
    public static string ToApiErrorCode(this AuthStatus status) => status switch {
        AuthStatus.Success => string.Empty,
        AuthStatus.InvalidInput => "INVALID_PACKET",
        AuthStatus.InvalidCredentials => "INVALID_CREDENTIALS",
        AuthStatus.InvalidMachineId => "INVALID_MACHINE_ID",
        AuthStatus.AccountMachineMismatch => "ACCOUNT_MACHINE_MISMATCH",
        AuthStatus.AccountDisabled => "ACCOUNT_DISABLED",
        AuthStatus.RoleMismatch => "SERVER_ERROR",
        AuthStatus.ServerError => "SERVER_ERROR",
        _ => "SERVER_ERROR"
    };
}
