using ServerApp.Auth.Models;
using ServerApp.Database.Entities;

namespace ServerApp.Auth.Contracts;

public interface ISessionService{
    Task<SessionInfo> OpenSessionAsync(UserRecord user, CancellationToken cancellationToken = default);

    Task CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<SessionInfo?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default);
}
