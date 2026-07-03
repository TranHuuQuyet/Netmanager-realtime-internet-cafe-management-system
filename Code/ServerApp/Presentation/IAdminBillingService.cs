// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Interface cho cac thao tac billing ma man hinh admin can goi.
public interface IAdminBillingService
{
    // Event phat ra khi billing cua may thay doi de UI cap nhat.
    event Action<AdminBillingResult>? BillingUpdated;

    // Bat dau thue may co gio ket thuc.
    Task<AdminBillingResult> StartTimedAsync(
        string machineId,
        int durationMinutes,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    // Bat dau thue may khong gioi han thoi gian.
    Task<AdminBillingResult> StartOpenEndedAsync(
        string machineId,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    // Dam bao may co phien open-ended; neu da co thi chi dong bo lai.
    Task<AdminBillingResult> EnsureOpenEndedAsync(
        string machineId,
        long ratePerHour = 10_000,
        CancellationToken cancellationToken = default);

    // Gia han them phut cho may.
    Task<AdminBillingResult> ExtendAsync(
        string machineId,
        int extensionMinutes,
        CancellationToken cancellationToken = default);

    // Dong phien billing cua may.
    Task<AdminBillingResult> CloseAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    // Nap them tien vao tai khoan khach dang dung may.
    Task<AdminBillingResult> TopUpMachineAsync(
        string machineId,
        long amountVnd,
        CancellationToken cancellationToken = default);

    // Dong bo timer cua mot may neu may co phien active.
    Task<AdminBillingResult?> SyncMachineAsync(
        string machineId,
        CancellationToken cancellationToken = default);

    // Dong bo lai tat ca phien active.
    Task<IReadOnlyList<AdminBillingResult>> RefreshActiveSessionsAsync(
        CancellationToken cancellationToken = default);
}
