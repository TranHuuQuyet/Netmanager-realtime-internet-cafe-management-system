using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

public interface ISessionRepository {
    Task AddAsync(SessionRecord session, CancellationToken cancellationToken = default);

    Task<SessionRecord?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<SessionRecord?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default);

    Task RevokeActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task UpdateStateAsync(string sessionId, SessionState state, DateTimeOffset endedAtUtc, CancellationToken cancellationToken = default);
}
