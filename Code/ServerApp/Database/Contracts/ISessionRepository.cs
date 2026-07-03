using ServerApp.Auth.Models;
using ServerApp.Database.Models;

// Namespace chua cac interface truy cap du lieu database.
namespace ServerApp.Database.Contracts;

// Interface mo ta cac thao tac voi phien dang nhap/xac thuc.
public interface ISessionRepository
{
    // Them phien dang nhap moi.
    Task AddAsync(
        SessionRecord session,
        CancellationToken cancellationToken = default);

    // Lay phien active cua user theo userId.
    Task<SessionRecord?> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Lay phien active tren mot may.
    Task<SessionRecord?> GetActiveByMachineIdAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    // Lay phien theo Id.
    Task<SessionRecord?> GetByIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    // Thu hoi tat ca phien active cua mot user.
    Task RevokeActiveSessionsByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Cap nhat trang thai phien va thoi diem ket thuc.
    Task UpdateStateAsync(
        string sessionId,
        SessionState state,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default);
}
