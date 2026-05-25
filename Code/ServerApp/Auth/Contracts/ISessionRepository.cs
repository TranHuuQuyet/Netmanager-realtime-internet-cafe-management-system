using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

// Luu/lay session o tang du lieu; khong chua logic nghiep vu.
public interface ISessionRepository {
    // Ghi mot session moi vao DB khi user dang nhap thanh cong.
    Task AddAsync(SessionRecord session, CancellationToken cancellationToken = default);

    // Tim session dang hoat dong gan nhat cua mot user.
    Task<SessionRecord?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    // Lay session theo Id de phuc vu dong session hoac debug.
    Task<SessionRecord?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default);

    // Huy tat ca session con active cua user truoc khi mo session moi.
    Task RevokeActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    // Cap nhat trang thai session khi dong/closed/revoked.
    Task UpdateStateAsync(string sessionId, SessionState state, DateTimeOffset endedAtUtc, CancellationToken cancellationToken = default);
}
