using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using AuthSessionState = ServerApp.Auth.Models.SessionState;
using AuthUserRole = ServerApp.Auth.Models.UserRole;

namespace ServerApp.Auth.Services;

// Quan ly vong doi session: mo session moi, dong session va doc session active.
public sealed class SessionService : ISessionService {
    private readonly ISessionRepository _sessions;
    private readonly IMachineRepository _machines;

    public SessionService(ISessionRepository sessions, IMachineRepository machines) {
        _sessions = sessions;
        _machines = machines;
    }

    // Khi user login thanh cong, revoke session cu roi tao session moi de tranh dang nhap tron.
    public async Task<SessionInfo> OpenSessionAsync(UserRecord user, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(user.Id)) {
            throw new ArgumentException("User id is required.", nameof(user));
        }

        try {
            var startedAtUtc = DateTimeOffset.UtcNow;

            await _sessions.RevokeActiveSessionsByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);

            var record = new SessionRecord(
                Guid.NewGuid().ToString("N"),
                user.Id,
                user.Username,
                user.Role,
                user.MachineId,
                AuthSessionState.Active,
                startedAtUtc,
                null);

            await _sessions.AddAsync(record, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(record.MachineId)) {
                await _machines.UpdateStatusAsync(
                        record.MachineId,
                        MachineStatusOnline,
                        startedAtUtc.UtcDateTime,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ToSessionInfo(record);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new InvalidOperationException("Failed to open session.", ex);
        }
    }

    // Dong session neu co sessionId hop le; neu sessionId rong thi bo qua an toan.
    public async Task CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default) {
        try {
            if (string.IsNullOrWhiteSpace(sessionId)) {
                return;
            }

            var endedAtUtc = DateTimeOffset.UtcNow;
            var session = await _sessions.GetByIdAsync(sessionId.Trim(), cancellationToken).ConfigureAwait(false);
            await _sessions.UpdateStateAsync(sessionId.Trim(), AuthSessionState.Closed, endedAtUtc, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(session?.MachineId)) {
                await _machines.UpdateStatusAsync(
                        session.MachineId,
                        MachineStatusOffline,
                        endedAtUtc.UtcDateTime,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new InvalidOperationException("Failed to close session.", ex);
        }
    }

    // Lay session active hien tai cua user de server/UI co the kiem tra trang thai dang hoat dong.
    public async Task<SessionInfo?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return null;
        }

        try {
            var record = await _sessions.GetActiveByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return record is null ? null : ToSessionInfo(record);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new InvalidOperationException("Failed to load active session.", ex);
        }
    }

    public async Task<AuthResult> AuthorizeCommandTargetAsync(string machineId, CancellationToken cancellationToken = default) {
        machineId = Normalize(machineId);

        if (string.IsNullOrWhiteSpace(machineId)) {
            return AuthResult.Failure(AuthStatus.UnauthorizedCommand, "Command target machine is required.");
        }

        try {
            var machine = await _machines.GetByMachineIdAsync(machineId, cancellationToken).ConfigureAwait(false);
            if (machine is null || !machine.IsActive) {
                return AuthResult.Failure(AuthStatus.UnauthorizedCommand, "Command target is not available.");
            }

            var activeSession = await _sessions.GetActiveByMachineIdAsync(machineId, cancellationToken).ConfigureAwait(false);
            if (activeSession is null || !string.Equals(activeSession.MachineId, machineId, StringComparison.OrdinalIgnoreCase)) {
                return AuthResult.Failure(AuthStatus.UnauthorizedCommand, "Command target has no active session.");
            }

            var user = new UserSummary(
                activeSession.UserId,
                activeSession.Username,
                (AuthUserRole)activeSession.Role,
                activeSession.MachineId ?? string.Empty,
                true,
                activeSession.StartedAtUtc);

            return AuthResult.Success(user, ToSessionInfo(activeSession), "Command target accepted.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new InvalidOperationException("Failed to authorize command target.", ex);
        }
    }

    // Chuyen SessionRecord trong DB thanh SessionInfo domain object.
    private static SessionInfo ToSessionInfo(SessionRecord record)
        => new(
            record.Id,
            record.UserId,
            record.Username,
            (AuthUserRole)record.Role,
            record.MachineId ?? string.Empty,
            (AuthSessionState)record.State,
            record.StartedAtUtc,
            record.EndedAtUtc);

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private const string MachineStatusOnline = "Online";
    private const string MachineStatusOffline = "Offline";
}
