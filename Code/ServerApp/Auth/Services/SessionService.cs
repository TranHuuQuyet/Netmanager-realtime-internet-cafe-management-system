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

    public SessionService(ISessionRepository sessions) {
        _sessions = sessions;
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

            await _sessions.UpdateStateAsync(sessionId.Trim(), AuthSessionState.Closed, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
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
}
