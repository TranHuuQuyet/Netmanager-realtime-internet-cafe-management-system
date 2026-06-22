using ServerApp.Billing.Models;

namespace ServerApp.Billing.Contracts;

public interface IBillingService
{
    Task<BillingResult> OpenSessionAsync(
        BillingSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<BillingResult> ExtendSessionAsync(
        string billingSessionId,
        TimeSpan extension,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<BillingResult> CloseSessionAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default);

    Task<BillingResult?> GetActiveSessionAsync(
        string machineId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<BillingRecoverySnapshot> GetRecoverySnapshotAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    BillingCalculation CalculateAmount(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000);
}
