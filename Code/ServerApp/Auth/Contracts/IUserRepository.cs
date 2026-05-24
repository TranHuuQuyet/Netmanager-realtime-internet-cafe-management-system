using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

// Truy xuat du lieu user trong DB, chi xu ly storage va query.
public interface IUserRepository {
    // Tim user theo username de phuc vu dang nhap.
    Task<UserRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    // Tim user theo Id khi can cap nhat hoac kiem tra lai thong tin.
    Task<UserRecord?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    // Dem so luong user hien co, thuong dung cho seed hoac smoke check.
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    // Chen moi hoac ghi de mot ban ghi user.
    Task AddAsync(UserRecord user, CancellationToken cancellationToken = default);

    // Cap nhat lan dang nhap cuoi de phuc vu theo doi trang thai.
    Task UpdateLastLoginAtAsync(string userId, DateTimeOffset lastLoginAtUtc, CancellationToken cancellationToken = default);
}
