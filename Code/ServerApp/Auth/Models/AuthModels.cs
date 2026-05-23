namespace ServerApp.Auth.Models;

public enum UserRole {
    Admin = 0,
    Client = 1
}

public enum AuthStatus {
    Success = 0,
    InvalidInput = 1,
    InvalidCredentials = 2,
    InvalidMachineId = 3,
    AccountMachineMismatch = 4,
    AccountDisabled = 5,
    RoleMismatch = 6,
    ServerError = 7
}

public enum SessionState {
    Active = 0,
    Closed = 1,
    Revoked = 2
}

public sealed record AuthRequest(string Username, string Password, string MachineId, UserRole? RequiredRole = null);

public sealed record AuthResult {
    public required bool IsSuccess { get; init; }

    public required AuthStatus Status { get; init; }

    public required string Message { get; init; }

    public UserSummary? User { get; init; }

    public SessionInfo? Session { get; init; }

    public static AuthResult Success(UserSummary user, SessionInfo session, string message = "Login succeeded.")
        => new() {
            IsSuccess = true,
            Status = AuthStatus.Success,
            Message = message,
            User = user,
            Session = session
        };

    public static AuthResult Failure(AuthStatus status, string message)
        => new() {
            IsSuccess = false,
            Status = status,
            Message = message
        };
}

public sealed record UserSummary(
    string Id,
    string Username,
    UserRole Role,
    string MachineId,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);

public sealed record SessionInfo(
    string Id,
    string UserId,
    string Username,
    UserRole Role,
    string MachineId,
    SessionState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record PasswordHash(string SaltBase64, string HashBase64);

public sealed record SeedAccount(string Username, string Password, string MachineId, UserRole Role, bool IsActive = true);
