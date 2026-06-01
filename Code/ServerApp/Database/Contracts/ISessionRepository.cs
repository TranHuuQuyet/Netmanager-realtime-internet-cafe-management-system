using ServerApp.Auth.Models;
using ServerApp.Database.Models;

namespace ServerApp.Database.Contracts;

public interface ISessionRepository
{
    Task AddAsync(
        SessionRecord session,
        CancellationToken cancellationToken = default);

    Task<SessionRecord?> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SessionRecord?> GetByIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task RevokeActiveSessionsByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task UpdateStateAsync(
        string sessionId,
        SessionState state,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default);
}