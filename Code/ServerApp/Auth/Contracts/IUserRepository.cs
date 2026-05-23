using ServerApp.Database.Entities;

namespace ServerApp.Auth.Contracts;

public interface IUserRepository {
    Task<UserRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<UserRecord?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task AddAsync(UserRecord user, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAtAsync(string userId, DateTimeOffset lastLoginAtUtc, CancellationToken cancellationToken = default);
}
