using ServerApp.Billing.Models;

// Namespace chua cac hop dong cua nghiep vu tinh tien.
namespace ServerApp.Billing.Contracts;

// Interface dinh nghia cac chuc nang tinh tien ma tang UI/service khac co the goi.
public interface IBillingService
{
    // Mo mot phien tinh tien moi cho mot may/user.
    Task<BillingResult> OpenSessionAsync(
        BillingSessionRequest request,
        CancellationToken cancellationToken = default);

    // Gia han thoi gian cho phien tinh tien dang co.
    Task<BillingResult> ExtendSessionAsync(
        string billingSessionId,
        TimeSpan extension,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    // Dong phien tinh tien va tinh so tien cuoi cung.
    Task<BillingResult> CloseSessionAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default);

    // Lay phien tinh tien dang active cua mot may tai thoi diem hien tai.
    Task<BillingResult?> GetActiveSessionAsync(
        string machineId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    // Lay snapshot cac phien dang active de khoi phuc dong bo khi server mo lai.
    Task<BillingRecoverySnapshot> GetRecoverySnapshotAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    // Tinh tien dua tren thoi diem bat dau, thoi diem hien tai va don gia theo gio.
    BillingCalculation CalculateAmount(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000);
}
