using ServerApp.Billing.Contracts;
using ServerApp.Billing.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;

// Namespace chua cac service billing.
namespace ServerApp.Billing.Services;

// Service xu ly nghiep vu tinh tien: mo phien, gia han, dong phien, khoi phuc phien active.
public sealed class BillingService : IBillingService
{
    // Repository dung de luu va doc BillingSessionRecord tu database.
    private readonly IBillingSessionRepository _billingSessions;

    // Constructor nhan repository qua dependency injection thu cong.
    public BillingService(IBillingSessionRepository billingSessions)
    {
        _billingSessions = billingSessions;
    }

    // Ham boc cong thuc tinh tien de cac lop khac khong can goi BillingCalculator truc tiep.
    public BillingCalculation CalculateAmount(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000)
        => BillingCalculator.Calculate(startedAtUtc, asOfUtc, ratePerHour);

    // Mo phien tinh tien moi cho user tren mot may.
    public async Task<BillingResult> OpenSessionAsync(
        BillingSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Kiem tra cac thong tin bat buoc de tranh tao session thieu du lieu.
        if (string.IsNullOrWhiteSpace(request.AuthSessionId)
            || string.IsNullOrWhiteSpace(request.UserId)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.MachineId))
        {
            return BillingResult.Failure(
                "INVALID_BILLING_SESSION",
                "Billing session requires auth session, user and machine metadata.");
        }

        // Mot may chi nen co mot phien billing active tai mot thoi diem.
        var existing = await _billingSessions.GetActiveByMachineIdAsync(request.MachineId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return BillingResult.Failure(
                "BILLING_SESSION_ALREADY_ACTIVE",
                "Machine already has an active billing session.");
        }

        // Tao record billing moi voi trang thai Active.
        var now = request.StartedAtUtc;
        var session = new BillingSessionRecord(
            Guid.NewGuid().ToString("N"),
            request.AuthSessionId.Trim(),
            request.UserId.Trim(),
            request.Username.Trim(),
            request.MachineId.Trim(),
            request.RentalMode,
            BillingSessionState.Active,
            request.RatePerHour <= 0 ? 10_000 : request.RatePerHour,
            now,
            request.ExpiresAtUtc,
            null,
            0,
            0);

        // Luu session vao repository/database.
        await _billingSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);

        // Luc moi mo thi tinh tien tai chinh thoi diem bat dau.
        var calculation = CalculateAmount(session.StartedAtUtc, session.StartedAtUtc, session.RatePerHour);
        return BillingResult.Success(
            new BillingSessionView(session, calculation),
            "Billing session opened.");
    }

    // Gia han them thoi gian cho phien tinh tien.
    public async Task<BillingResult> ExtendSessionAsync(
        string billingSessionId,
        TimeSpan extension,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // Lay session theo Id; neu khong co thi tra loi that bai.
        var session = await _billingSessions.GetByIdAsync(billingSessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return BillingResult.Failure("BILLING_SESSION_NOT_FOUND", "Billing session was not found.");
        }

        // Tao ban sao session voi gio het han moi va tien hien tai.
        var updatedSession = session with
        {
            RentalMode = BillingRentalMode.Extend,
            ExpiresAtUtc = session.ExpiresAtUtc is null
                ? asOfUtc.Add(extension)
                : session.ExpiresAtUtc.Value.Add(extension),
            ChargedMinutes = CalculateAmount(session.StartedAtUtc, asOfUtc, session.RatePerHour).ChargedMinutes,
            AmountVnd = CalculateAmount(session.StartedAtUtc, asOfUtc, session.RatePerHour).AmountVnd
        };

        // Luu ban session da gia han.
        await _billingSessions.UpdateAsync(updatedSession, cancellationToken).ConfigureAwait(false);

        return BillingResult.Success(
            new BillingSessionView(
                updatedSession,
                CalculateAmount(updatedSession.StartedAtUtc, asOfUtc, updatedSession.RatePerHour)),
            "Billing session extended.");
    }

    // Dong phien tinh tien va ghi lai so tien phai tra.
    public async Task<BillingResult> CloseSessionAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default)
    {
        // Lay session can dong.
        var session = await _billingSessions.GetByIdAsync(billingSessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return BillingResult.Failure("BILLING_SESSION_NOT_FOUND", "Billing session was not found.");
        }

        // Tinh tien tai thoi diem ket thuc va cap nhat database.
        var calculation = CalculateAmount(session.StartedAtUtc, endedAtUtc, session.RatePerHour);
        await _billingSessions.CloseAsync(
                session.Id,
                endedAtUtc,
                calculation.ChargedMinutes,
                calculation.AmountVnd,
                cancellationToken)
            .ConfigureAwait(false);

        // Tao ban ghi ket qua da dong de tra ve cho caller.
        var closedSession = session with
        {
            State = BillingSessionState.Closed,
            EndedAtUtc = endedAtUtc,
            ChargedMinutes = calculation.ChargedMinutes,
            AmountVnd = calculation.AmountVnd
        };

        return BillingResult.Success(
            new BillingSessionView(closedSession, calculation),
            "Billing session closed.");
    }

    // Lay phien tinh tien dang active cua mot may, kem so tien cap nhat den asOfUtc.
    public async Task<BillingResult?> GetActiveSessionAsync(
        string machineId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var session = await _billingSessions.GetActiveByMachineIdAsync(machineId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return null;
        }

        // Cap nhat so phut va so tien hien tai truoc khi tra ve.
        var calculation = CalculateAmount(session.StartedAtUtc, asOfUtc, session.RatePerHour);
        var typedSession = session with
        {
            ChargedMinutes = calculation.ChargedMinutes,
            AmountVnd = calculation.AmountVnd
        };

        return BillingResult.Success(
            new BillingSessionView(typedSession, calculation),
            "Billing session restored.");
    }

    // Lay tat ca phien billing dang active de server/gui client dong bo lai sau khi khoi dong.
    public async Task<BillingRecoverySnapshot> GetRecoverySnapshotAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // Doc danh sach active session tu repository.
        IReadOnlyList<BillingSessionRecord> sessions = await _billingSessions.ListActiveAsync(cancellationToken).ConfigureAwait(false);
        var syncSessions = new List<BillingSyncSession>(sessions.Count);

        // Chuyen moi billing session thanh goi dong bo timer.
        foreach (BillingSessionRecord session in sessions)
        {
            syncSessions.Add(BillingCalculator.BuildSyncSession(session, asOfUtc));
        }

        return new BillingRecoverySnapshot(asOfUtc, syncSessions);
    }
}
