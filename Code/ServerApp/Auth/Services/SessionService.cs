using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp.Auth.Services;

public sealed class SessionService : ISessionService {
    private readonly ISessionRepository _sessions;

    public SessionService(ISessionRepository sessions) {
        _sessions = sessions;
    }

    public async Task<SessionInfo> OpenSessionAsync(UserRecord user, CancellationToken cancellationToken = default) {
        var startedAtUtc = DateTimeOffset.UtcNow;

        await _sessions.RevokeActiveSessionsByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);

        var record = new SessionRecord(
            Guid.NewGuid().ToString("N"),
            user.Id,
            user.Username,
            user.Role,
            user.MachineId,
            SessionState.Active,
            startedAtUtc,
            null);

        await _sessions.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return ToSessionInfo(record);
    }

    public async Task CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sessionId)) {
            return;
        }

        await _sessions.UpdateStateAsync(sessionId.Trim(), SessionState.Closed, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionInfo?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default) {
        var record = await _sessions.GetActiveByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return record is null ? null : ToSessionInfo(record);
    }

    private static SessionInfo ToSessionInfo(SessionRecord record)
        => new(record.Id, record.UserId, record.Username, record.Role, record.MachineId ?? string.Empty, record.State, record.StartedAtUtc, record.EndedAtUtc);
}
