using ServerApp.Database.Entities;

// Namespace gom cac hop dong va lop truy cap du lieu user.
namespace ServerApp.Database.Repositories;

// Interface dinh nghia cac thao tac toi thieu ma mot UserRepository can co.
// Lop nao implement interface nay phai viet code cho tat ca cac method ben duoi.
public interface IUserRepository
{
    // Tim user theo username.
    // Dau ? trong UserEntity? nghia la ket qua co the la null neu khong tim thay.
    Task<UserEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    // Cap nhat thoi diem dang nhap gan nhat cua user.
    // Guid userId la khoa dinh danh user can cap nhat.
    Task TouchLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default);
}
