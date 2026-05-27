using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task TouchLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default);
}
