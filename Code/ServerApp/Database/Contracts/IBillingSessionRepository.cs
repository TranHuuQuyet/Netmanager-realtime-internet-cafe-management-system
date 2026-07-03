using ServerApp.Database.Models;

// Namespace chua cac interface truy cap du lieu database.
namespace ServerApp.Database.Contracts;

// Interface mo ta cac thao tac luu/doc phien tinh tien.
public interface IBillingSessionRepository
{
    // Them phien tinh tien moi.
    Task AddAsync(
        BillingSessionRecord session,
        CancellationToken cancellationToken = default);

    // Lay phien tinh tien theo Id.
    Task<BillingSessionRecord?> GetByIdAsync(
        string billingSessionId,
        CancellationToken cancellationToken = default);

    // Lay phien tinh tien active theo ma may.
    Task<BillingSessionRecord?> GetActiveByMachineIdAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    // Lay tat ca phien tinh tien dang active.
    Task<IReadOnlyList<BillingSessionRecord>> ListActiveAsync(
        CancellationToken cancellationToken = default);

    // Cap nhat toan bo thong tin phien tinh tien.
    Task UpdateAsync(
        BillingSessionRecord session,
        CancellationToken cancellationToken = default);

    // Dong phien tinh tien va ghi lai tong phut/tong tien.
    Task CloseAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        long chargedMinutes,
        long amountVnd,
        CancellationToken cancellationToken = default);
}
