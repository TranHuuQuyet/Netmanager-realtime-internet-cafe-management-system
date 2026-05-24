using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public sealed class SeedUserRepository : IUserRepository
{
    private readonly List<UserEntity> _users;

    public SeedUserRepository(IEnumerable<UserEntity>? users = null)
    {
        _users = (users ?? SeedData.Users).ToList();
    }

    public Task<UserEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task TouchLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Id == userId);
        if (index >= 0)
        {
            _users[index] = _users[index] with { LastLogin = loginTime };
        }

        return Task.CompletedTask;
    }
}
