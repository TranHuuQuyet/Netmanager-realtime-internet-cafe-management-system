namespace ServerApp.Auth.Models;

// Role xac dinh quyen dang nhap va cach xu ly login o server.
public enum UserRole {
    Admin = 0,
    Client = 1
}

// Ma ket qua chuan hoa cho toan bo luong auth.
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

// Trang thai vong doi cua session trong DB.
public enum SessionState {
    Active = 0,
    Closed = 1,
    Revoked = 2
}

// Du lieu client gui len khi muon dang nhap.
public sealed record AuthRequest(string Username, string Password, string MachineId, UserRole? RequiredRole = null);

// Ket qua tra ve sau xac thuc: co the mang ca user summary va session.
public sealed record AuthResult {
    public required bool IsSuccess { get; init; }

    public required AuthStatus Status { get; init; }

    public required string Message { get; init; }

    public UserSummary? User { get; init; }

    public SessionInfo? Session { get; init; }

    // Tao result thanh cong de caller khong can tu gom fields.
    public static AuthResult Success(UserSummary user, SessionInfo session, string message = "Login succeeded.")
        => new() {
            IsSuccess = true,
            Status = AuthStatus.Success,
            Message = message,
            User = user,
            Session = session
        };

    // Tao result that bai voi status va message ro rang.
    public static AuthResult Failure(AuthStatus status, string message)
        => new() {
            IsSuccess = false,
            Status = status,
            Message = message
        };
}

// Ban tom tat user de UI/network khong can doc toan bo ban ghi DB.
public sealed record UserSummary(
    string Id,
    string Username,
    UserRole Role,
    string MachineId,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);

// Thong tin session ma auth service tra ve sau khi mo session.
public sealed record SessionInfo(
    string Id,
    string UserId,
    string Username,
    UserRole Role,
    string MachineId,
    SessionState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

// Hash va salt duoc luu trong DB de so sanh mat khau an toan.
public sealed record PasswordHash(string SaltBase64, string HashBase64);
