using ServerApp.Database.Models;

namespace ServerApp.Database.Contracts;

public interface IBillingSessionRepository
{
    Task AddAsync(
        BillingSessionRecord session,
        CancellationToken cancellationToken = default);

    Task<BillingSessionRecord?> GetByIdAsync(
        string billingSessionId,
        CancellationToken cancellationToken = default);

    Task<BillingSessionRecord?> GetActiveByMachineIdAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingSessionRecord>> ListActiveAsync(
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BillingSessionRecord session,
        CancellationToken cancellationToken = default);

    Task CloseAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        long chargedMinutes,
        long amountVnd,
        CancellationToken cancellationToken = default);
}
