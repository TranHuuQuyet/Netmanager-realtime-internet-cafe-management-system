using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp.Auth.Services;

public sealed class AuthService : IAuthService {
    private const string AdminMachineId = "PC00";

    private readonly IUserRepository _users;
    private readonly ISessionService _sessions;

    public AuthService(IUserRepository users, ISessionService sessions) {
        _users = users;
        _sessions = sessions;
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default) {
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
            var user = await _users.GetByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
            if (user is null){
                return AuthResult.Failure(AuthStatus.InvalidCredentials, "Account was not found.");
            }

            if (!user.IsActive){
                return AuthResult.Failure(AuthStatus.AccountDisabled, "Account is inactive.");
            }

            if (request.RequiredRole.HasValue && user.Role != request.RequiredRole.Value) {
                return AuthResult.Failure(AuthStatus.RoleMismatch, "Account role is not allowed for this login.");
            }

            if (!PasswordHasher.Verify(password, user.PasswordSaltBase64, user.PasswordHashBase64)) {
                return AuthResult.Failure(AuthStatus.InvalidCredentials, "Password is incorrect.");
            }

            if (user.Role == UserRole.Admin) {
                if (string.IsNullOrWhiteSpace(machineId)) {
                    return AuthResult.Failure(AuthStatus.InvalidMachineId, "Admin machine ID is required.");
                }

                if (!string.Equals(machineId, AdminMachineId, StringComparison.OrdinalIgnoreCase)) {
                    return AuthResult.Failure(AuthStatus.AccountMachineMismatch, "Admin account is only allowed on PC00.");
                }
            }
            else {
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

            var session = await _sessions.OpenSessionAsync(user, cancellationToken).ConfigureAwait(false);
            await _users.UpdateLastLoginAtAsync(user.Id, session.StartedAtUtc, cancellationToken).ConfigureAwait(false);

            var summary = new UserSummary(
                user.Id,
                user.Username,
                user.Role,
                user.MachineId ?? string.Empty,
                user.IsActive,
                session.StartedAtUtc);

            return AuthResult.Success(summary, session);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            return AuthResult.Failure(AuthStatus.ServerError, "Authentication failed due to a server error.");
        }
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
