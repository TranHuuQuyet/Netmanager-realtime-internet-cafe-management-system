using ServerApp.Database.Models;

// Namespace chua cac model rieng cua nghiep vu tinh tien.
namespace ServerApp.Billing.Models;

// Du lieu dau vao khi mo mot phien tinh tien moi.
// AuthSessionId lien ket phien tinh tien voi phien dang nhap/xac thuc.
public sealed record BillingSessionRequest(
    string AuthSessionId,
    string UserId,
    string Username,
    string MachineId,
    BillingRentalMode RentalMode,
    DateTimeOffset StartedAtUtc,
    long RatePerHour = 10_000,
    DateTimeOffset? ExpiresAtUtc = null);

// Ket qua tinh tien tai mot thoi diem.
// ChargedMinutes la so phut da tinh phi, AmountVnd la so tien VND.
public sealed record BillingCalculation(
    long ChargedMinutes,
    long RatePerHour,
    long AmountVnd,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset AsOfUtc);

// Goi ban ghi billing session kem ket qua tinh tien hien tai.
public sealed record BillingSessionView(
    BillingSessionRecord Session,
    BillingCalculation Calculation);

// Du lieu dung de dong bo timer ve client.
// RemainingSeconds co gia tri khi session co gio het han.
// ShouldLockNow bao server biet da den luc khoa may hay chua.
public sealed record BillingSyncSession(
    BillingSessionView Session,
    long? RemainingSeconds,
    bool ShouldLockNow);

// Snapshot cac phien dang chay khi server can khoi phuc/dong bo lai trang thai.
public sealed record BillingRecoverySnapshot(
    DateTimeOffset RestoredAtUtc,
    IReadOnlyList<BillingSyncSession> Sessions);

// Ket qua tra ve tu cac thao tac billing.
// Dung mot kieu chung de biet thao tac thanh cong hay that bai va co session nao lien quan.
public sealed record BillingResult
{
    // true neu thao tac billing thanh cong.
    public required bool IsSuccess { get; init; }

    // Thong bao de hien thi/log.
    public required string Message { get; init; }

    // Du lieu session neu thao tac thanh cong hoac co session can tra ve.
    public BillingSessionView? Session { get; init; }

    // Ma loi ngan gon cho code UI/service xu ly nhanh.
    public string? ErrorCode { get; init; }

    // Thuoc tinh tien ich: that bai la nguoc lai cua thanh cong.
    public bool IsFailure => !IsSuccess;

    // Tao ket qua thanh cong.
    public static BillingResult Success(
        BillingSessionView session,
        string message = "Billing session accepted.")
        => new()
        {
            IsSuccess = true,
            Message = message,
            Session = session
        };

    // Tao ket qua that bai kem ma loi va thong bao.
    public static BillingResult Failure(
        string errorCode,
        string message)
        => new()
        {
            IsSuccess = false,
            Message = message,
            ErrorCode = errorCode
        };
}

// Lop tien ich gom cac cong thuc tinh tien va tao du lieu dong bo timer.
public static class BillingCalculator
{
    // Tinh so phut va so tien tu thoi diem bat dau den thoi diem asOfUtc.
    public static BillingCalculation Calculate(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000)
    {
        // Neu thoi diem tinh tien nho hon luc bat dau, ep ve luc bat dau de tranh so am.
        if (asOfUtc < startedAtUtc)
        {
            asOfUtc = startedAtUtc;
        }

        // Tinh so giay da qua, lam tron len phut tinh phi, roi tinh tien theo don gia/gio.
        // Vi du: 61 giay duoc tinh la 2 phut, con 0 giay van la 0 phut.
        double elapsedSeconds = (asOfUtc - startedAtUtc).TotalSeconds;
        long chargedMinutes = elapsedSeconds <= 0
            ? 0
            : (long)Math.Ceiling(elapsedSeconds / 60.0);
        long amountVnd = (long)Math.Ceiling(chargedMinutes * ratePerHour / 60.0);

        return new BillingCalculation(
            chargedMinutes,
            ratePerHour,
            amountVnd,
            startedAtUtc,
            asOfUtc);
    }

    // Tao goi du lieu gui ve client de cap nhat timer va quyet dinh co can khoa may khong.
    public static BillingSyncSession BuildSyncSession(
        BillingSessionRecord session,
        DateTimeOffset asOfUtc)
    {
        // Tinh tien hien tai cua session.
        BillingCalculation calculation = Calculate(session.StartedAtUtc, asOfUtc, session.RatePerHour);

        long? remainingSeconds = null;
        bool shouldLockNow = false;

        // Neu session co gio het han thi tinh so giay con lai.
        if (session.ExpiresAtUtc is not null)
        {
            TimeSpan remaining = session.ExpiresAtUtc.Value - asOfUtc;
            remainingSeconds = remaining > TimeSpan.Zero
                ? (long)Math.Ceiling(remaining.TotalSeconds)
                : 0;
            shouldLockNow = remainingSeconds == 0;
        }

        // Tao ban sao session da cap nhat so phut va so tien hien tai.
        BillingSessionRecord updatedSession = session with
        {
            ChargedMinutes = calculation.ChargedMinutes,
            AmountVnd = calculation.AmountVnd
        };

        return new BillingSyncSession(
            new BillingSessionView(updatedSession, calculation),
            remainingSeconds,
            shouldLockNow);
    }
}
