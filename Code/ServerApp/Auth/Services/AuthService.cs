using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using AuthUserRole = ServerApp.Auth.Models.UserRole;

namespace ServerApp.Auth.Services;

// Xu ly logic dang nhap cap nghiep vu: check input, user, password, machine va mo session.
public sealed class AuthService : IAuthService {
    private const string AdminMachineId = "PC00";

    private readonly IUserRepository _users;
    private readonly ISessionService _sessions;

    public AuthService(IUserRepository users, ISessionService sessions) {
        _users = users;
        _sessions = sessions;
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default) {
        // Buoc 1: chan request null va chuan hoa du lieu dau vao.
        if (request is null) {
            return AuthResult.Failure(AuthStatus.InvalidInput, "Login request is required.");
        }

        var username = Normalize(request.Username);
        var password = request.Password ?? string.Empty;
        var machineId = Normalize(request.MachineId);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
            return AuthResult.Failure(AuthStatus.InvalidInput, "Username and password are required.");
        }

        try {
            // Buoc 2: tim user trong DB va loai bo cac truong hop khong hop le som.
            var user = await _users.GetByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
            if (user is null) {
                return AuthResult.Failure(AuthStatus.InvalidCredentials, "Account was not found.");
            }

            if (!user.IsActive) {
                return AuthResult.Failure(AuthStatus.AccountDisabled, "Account is inactive.");
            }

            if (request.RequiredRole.HasValue && (AuthUserRole)user.Role != request.RequiredRole.Value) {
                return AuthResult.Failure(AuthStatus.RoleMismatch, "Account role is not allowed for this login.");
            }

            // Buoc 3: so khop mat khau bang hash PBKDF2, khong bao gio so sanh plain text.
            if (!PasswordHasher.Verify(password, user.PasswordSaltBase64, user.PasswordHashBase64)) {
                return AuthResult.Failure(AuthStatus.InvalidCredentials, "Password is incorrect.");
            }

            // Buoc 4: kiem tra machineId theo role.
            if (user.Role == AuthUserRole.Admin) {
                // Admin chi duoc login tren may dinh danh PC00.
                if (string.IsNullOrWhiteSpace(machineId)) {
                    return AuthResult.Failure(AuthStatus.InvalidMachineId, "Admin machine ID is required.");
                }

                if (!string.Equals(machineId, AdminMachineId, StringComparison.OrdinalIgnoreCase)) {
                    return AuthResult.Failure(AuthStatus.AccountMachineMismatch, "Admin account is only allowed on PC00.");
                }
            }
            else {
                // Client bat buoc phai co machineId khop voi mapping trong DB.
                if (string.IsNullOrWhiteSpace(machineId)) {
                    return AuthResult.Failure(AuthStatus.InvalidMachineId, "Machine ID is required for client accounts.");
                }

                if (string.IsNullOrWhiteSpace(user.MachineId)) {
                    return AuthResult.Failure(AuthStatus.InvalidMachineId, "This account has no machine assignment.");
                }

                if (!string.Equals(user.MachineId, machineId, StringComparison.OrdinalIgnoreCase)) {
                    return AuthResult.Failure(AuthStatus.AccountMachineMismatch, "This account is not assigned to the selected machine.");
                }
            }

            // Buoc 5: mo session moi va cap nhat lan dang nhap cuoi.
            var session = await _sessions.OpenSessionAsync(user, cancellationToken).ConfigureAwait(false);
            await _users.UpdateLastLoginAtAsync(user.Id, session.StartedAtUtc, cancellationToken).ConfigureAwait(false);

            // Buoc 6: tra ve thong tin rut gon cho UI/network.
            var summary = new UserSummary(
                user.Id,
                user.Username,
                (AuthUserRole)user.Role,
                user.MachineId ?? string.Empty,
                user.IsActive,
                session.StartedAtUtc);

            return AuthResult.Success(summary, session);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // Neu co loi DB/service bat ngo, tra ve loi server thay vi nem exception ra UI.
            return AuthResult.Failure(AuthStatus.ServerError, "Authentication failed due to a server error.");
        }
    }

    // Trim/null-safe helper de login khong bi loi vi khoang trang.
    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
