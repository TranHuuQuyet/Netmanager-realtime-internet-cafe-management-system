using ServerApp.Database.Entities;

// Namespace gom cac lop repository cua tang Database.
namespace ServerApp.Database.Repositories;

// Repository user dung du lieu seed/in-memory.
// Lop nay phu hop cho demo/test vi khong ghi vao database that.
public sealed class SeedUserRepository : IUserRepository
{
    // Danh sach user duoc giu trong RAM.
    private readonly List<UserEntity> _users;

    // Constructor nhan danh sach user tuy chon.
    // Neu khong truyen users, lop se lay du lieu mac dinh tu SeedData.Users.
    public SeedUserRepository(IEnumerable<UserEntity>? users = null)
    {
        _users = (users ?? SeedData.Users).ToList();
    }

    // Tim user theo username.
    public Task<UserEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // FirstOrDefault tra ve user dau tien thoa dieu kien, hoac null neu khong co.
        // StringComparison.OrdinalIgnoreCase giup dang nhap khong phan biet hoa thuong.
        var user = _users.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase));

        // Boc ket qua thanh Task de phu hop voi interface async.
        return Task.FromResult(user);
    }

    // Cap nhat thoi diem dang nhap gan nhat cho user.
    public Task TouchLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default)
    {
        // Tim vi tri user trong List bang Id.
        var index = _users.FindIndex(user => user.Id == userId);
        if (index >= 0)
        {
            // Dung "with" de tao ban sao user moi co LastLogin moi.
            _users[index] = _users[index] with { LastLogin = loginTime };
        }

        // Khong co du lieu can tra ve nen dung CompletedTask.
        return Task.CompletedTask;
    }
}
