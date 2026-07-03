namespace ServerApp.Presentation;

public interface IAdminBillingService
{
    event Action<AdminBillingResult>? BillingUpdated;

    Task<AdminBillingResult> StartTimedAsync(
        string machineId,
        int durationMinutes,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult> StartOpenEndedAsync(
        string machineId,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult> EnsureOpenEndedAsync(
        string machineId,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult> ExtendAsync(
        string machineId,
        int extensionMinutes,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult> CloseAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult> TopUpMachineAsync(
        string machineId,
        long amountVnd,
        CancellationToken cancellationToken = default);

    Task<AdminBillingResult?> SyncMachineAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminBillingResult>> RefreshActiveSessionsAsync(
        CancellationToken cancellationToken = default);
}
