using ServerApp.Database.Models;

// Namespace chua cac interface truy cap du lieu database.
namespace ServerApp.Database.Contracts;

// Interface mo ta cac thao tac doc/ghi user auth trong database.
public interface IUserRepository
{
    // Tim user theo username.
    Task<UserRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    // Tim user theo Id.
    Task<UserRecord?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    // Dem tong so user.
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    // Them user moi.
    Task AddAsync(UserRecord user, CancellationToken cancellationToken = default);

    // Cap nhat thoi diem dang nhap gan nhat.
    Task UpdateLastLoginAtAsync(string userId, DateTimeOffset lastLoginAtUtc, CancellationToken cancellationToken = default);
}
