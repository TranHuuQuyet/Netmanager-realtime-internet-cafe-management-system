using ServerApp.Billing.Contracts;
using ServerApp.Billing.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using ServerApp.Networking;
using Shared.Enums;
using Shared.DTOs.CommandPayloads;

// Namespace cua tang Presentation: noi UI admin voi service billing va network.
namespace ServerApp.Presentation;

// Service billing danh cho admin, co them kha nang gui timer/lock/unlock qua network.
public sealed class NetworkAdminBillingService : IAdminBillingService
{
    // Cau hinh mac dinh cho tinh tien va cac quy uoc he thong.
    private const long DefaultRatePerHour = 10_000;
    private const string ServerMachineId = "PC00";
    private const string CustomerAuthUserIdPrefix = "customer-";
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(5);

    // Cac dependency chinh: billing service, session repository, network server va customer store.
    private readonly IBillingService _billing;
    private readonly ISessionRepository _sessions;
    private readonly TcpJsonLineServer? _networkServer;
    private readonly ICustomerRepository? _customers;

    // Ghi nho billing session nao da gui lenh lock de tranh gui lap lai lien tuc.
    private readonly HashSet<string> _lockSentForBillingSessions = new(StringComparer.OrdinalIgnoreCase);

    // Constructor nhan cac dependency can thiet.
    public NetworkAdminBillingService(
        IBillingService billing,
        ISessionRepository sessions,
        TcpJsonLineServer? networkServer,
        ICustomerRepository? customers = null)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _networkServer = networkServer;
        _customers = customers;
    }

    // Event de UI biet billing cua may vua thay doi.
    public event Action<AdminBillingResult>? BillingUpdated;

    // Bat dau phien billing co thoi luong co dinh.
    public async Task<AdminBillingResult> StartTimedAsync(
        string machineId,
        int durationMinutes,
        long ratePerHour = DefaultRatePerHour,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        if (durationMinutes <= 0)
        {
            return Error(targetMachineId, "INVALID_BILLING_DURATION", "Billing duration must be greater than zero.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return await OpenAsync(
            targetMachineId,
            BillingRentalMode.Timed,
            now,
            now.AddMinutes(durationMinutes),
            NormalizeRatePerHour(ratePerHour),
            cancellationToken).ConfigureAwait(false);
    }

    // Bat dau phien billing khong co gio ket thuc.
    public async Task<AdminBillingResult> StartOpenEndedAsync(
        string machineId,
        long ratePerHour = DefaultRatePerHour,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        return await OpenAsync(
            targetMachineId,
            BillingRentalMode.OpenEnded,
            DateTimeOffset.UtcNow,
            null,
            NormalizeRatePerHour(ratePerHour),
            cancellationToken).ConfigureAwait(false);
    }

    // Neu may da co billing active thi dong bo lai; neu chua co thi mo phien open-ended.
    public async Task<AdminBillingResult> EnsureOpenEndedAsync(
        string machineId,
        long ratePerHour = DefaultRatePerHour,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        BillingResult? active = await _billing.GetActiveSessionAsync(
            targetMachineId,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (active?.Session is not null)
        {
            return await PublishAsync(
                BillingCalculator.BuildSyncSession(active.Session.Session, DateTimeOffset.UtcNow),
                "Billing session synced.",
                cancellationToken).ConfigureAwait(false);
        }

        return await OpenAsync(
            targetMachineId,
            BillingRentalMode.OpenEnded,
            DateTimeOffset.UtcNow,
            null,
            NormalizeRatePerHour(ratePerHour),
            cancellationToken).ConfigureAwait(false);
    }

    // Gia han billing dang active cua may.
    public async Task<AdminBillingResult> ExtendAsync(
        string machineId,
        int extensionMinutes,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        if (extensionMinutes <= 0)
        {
            return Error(targetMachineId, "INVALID_BILLING_DURATION", "Billing extension must be greater than zero.");
        }

        BillingResult? active = await _billing.GetActiveSessionAsync(
            targetMachineId,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (active?.Session is null)
        {
            return Error(targetMachineId, "BILLING_SESSION_NOT_FOUND", "Machine has no active billing session.");
        }

        BillingResult extended = await _billing.ExtendSessionAsync(
            active.Session.Session.Id,
            TimeSpan.FromMinutes(extensionMinutes),
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (extended.IsFailure || extended.Session is null)
        {
            return Error(targetMachineId, extended.ErrorCode ?? "BILLING_EXTEND_FAILED", extended.Message);
        }

        _lockSentForBillingSessions.Remove(extended.Session.Session.Id);
        return await PublishAsync(
            BillingCalculator.BuildSyncSession(extended.Session.Session, DateTimeOffset.UtcNow),
            "Billing session extended.",
            cancellationToken).ConfigureAwait(false);
    }

    // Dong billing session dang active cua may.
    public async Task<AdminBillingResult> CloseAsync(
        string machineId,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        BillingResult? active = await _billing.GetActiveSessionAsync(
            targetMachineId,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (active?.Session is null)
        {
            return Error(targetMachineId, "BILLING_SESSION_NOT_FOUND", "Machine has no active billing session.");
        }

        BillingResult closed = await _billing.CloseSessionAsync(
            active.Session.Session.Id,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (closed.IsFailure || closed.Session is null)
        {
            return Error(targetMachineId, closed.ErrorCode ?? "BILLING_CLOSE_FAILED", closed.Message);
        }

        _lockSentForBillingSessions.Remove(closed.Session.Session.Id);
        var sync = new BillingSyncSession(
            closed.Session,
            RemainingSeconds: 0,
            ShouldLockNow: false);
        return await PublishAsync(sync, "Billing session closed.", cancellationToken).ConfigureAwait(false);
    }

    // Nap tien vao tai khoan khach dang dung may, sau do dong bo lai timer va unlock neu can.
    public async Task<AdminBillingResult> TopUpMachineAsync(
        string machineId,
        long amountVnd,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        if (amountVnd <= 0)
        {
            return Error(targetMachineId, "INVALID_TOP_UP_AMOUNT", "Top-up amount must be greater than zero.");
        }

        if (_customers is null)
        {
            return Error(targetMachineId, "CUSTOMER_STORE_UNAVAILABLE", "Customer store is unavailable.");
        }

        BillingResult? active = await _billing.GetActiveSessionAsync(
            targetMachineId,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (active?.Session is null)
        {
            return Error(targetMachineId, "BILLING_SESSION_NOT_FOUND", "Machine has no active billing session.");
        }

        if (!TryGetCustomerId(active.Session.Session.UserId, out string customerId))
        {
            return Error(targetMachineId, "CUSTOMER_ACCOUNT_NOT_FOUND", "Active session is not a customer account.");
        }

        CustomerRecord? customer = await _customers.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return Error(targetMachineId, "CUSTOMER_ACCOUNT_NOT_FOUND", "Customer account was not found.");
        }

        CustomerRecord updatedCustomer = customer with
        {
            AccountBalance = customer.AccountBalance + amountVnd
        };
        await _customers.UpdateAsync(updatedCustomer, cancellationToken).ConfigureAwait(false);

        return await PublishAsync(
            BillingCalculator.BuildSyncSession(active.Session.Session, DateTimeOffset.UtcNow),
            $"Topped up {amountVnd:N0} VND.",
            cancellationToken,
            unlockWhenAvailable: true).ConfigureAwait(false);
    }

    // Dong bo lai timer cho mot may neu may co billing session active.
    public async Task<AdminBillingResult?> SyncMachineAsync(
        string machineId,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (IsServerMachine(targetMachineId))
        {
            return null;
        }

        BillingResult? active = await _billing.GetActiveSessionAsync(
            targetMachineId,
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (active?.Session is null)
        {
            return null;
        }

        return await PublishAsync(
            BillingCalculator.BuildSyncSession(active.Session.Session, DateTimeOffset.UtcNow),
            "Billing session synced.",
            cancellationToken).ConfigureAwait(false);
    }

    // Quet tat ca billing session active va gui lai timer cho cac client.
    public async Task<IReadOnlyList<AdminBillingResult>> RefreshActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        BillingRecoverySnapshot snapshot = await _billing.GetRecoverySnapshotAsync(
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        var results = new List<AdminBillingResult>(snapshot.Sessions.Count);
        foreach (BillingSyncSession session in snapshot.Sessions)
        {
            if (IsServerMachine(session.Session.Session.MachineId))
            {
                continue;
            }

            results.Add(await PublishAsync(session, "Billing session refreshed.", cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    // Ham dung chung de mo billing session moi.
    private async Task<AdminBillingResult> OpenAsync(
        string targetMachineId,
        BillingRentalMode mode,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? expiresAtUtc,
        long ratePerHour,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetMachineId))
        {
            return Error(targetMachineId, "INVALID_MACHINE_ID", "Machine ID is required.");
        }

        if (IsServerMachine(targetMachineId))
        {
            return Error(targetMachineId, "SERVER_MACHINE_NOT_BILLABLE", "Server machine PC00 is not billable.");
        }

        SessionRecord? session = await _sessions.GetActiveByMachineIdAsync(
            targetMachineId,
            cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            return Error(targetMachineId, "NO_ACTIVE_AUTH_SESSION", "Machine has no active auth session.");
        }

        BillingResult opened = await _billing.OpenSessionAsync(
            new BillingSessionRequest(
                session.Id,
                session.UserId,
                session.Username,
                targetMachineId,
                mode,
                startedAtUtc,
                NormalizeRatePerHour(ratePerHour),
                expiresAtUtc),
            cancellationToken).ConfigureAwait(false);

        if (opened.IsFailure || opened.Session is null)
        {
            return Error(targetMachineId, opened.ErrorCode ?? "BILLING_OPEN_FAILED", opened.Message);
        }

        _lockSentForBillingSessions.Remove(opened.Session.Session.Id);
        return await PublishAsync(
            BillingCalculator.BuildSyncSession(opened.Session.Session, DateTimeOffset.UtcNow),
            "Billing session opened.",
            cancellationToken).ConfigureAwait(false);
    }

    // Gui TimerPayload qua network, tu dong lock/unlock may khi trang thai billing yeu cau.
    private async Task<AdminBillingResult> PublishAsync(
        BillingSyncSession sync,
        string message,
        CancellationToken cancellationToken,
        bool unlockWhenAvailable = false)
    {
        TimerPayload payload = await ToTimerPayloadAsync(sync, cancellationToken).ConfigureAwait(false);
        string machineId = payload.MachineId;

        if (_networkServer is not null)
        {
            await _networkServer.SendTimerAsync(machineId, payload, cancellationToken).ConfigureAwait(false);

            if (payload.ShouldLockNow
                && sync.Session.Session.State == BillingSessionState.Active
                && _lockSentForBillingSessions.Add(sync.Session.Session.Id))
            {
                await _networkServer.SendMachineCommandWithResultAsync(
                    machineId,
                    lockMachine: true,
                    issuedBy: "Billing",
                    reason: "Billing time or account balance expired.",
                    cancellationToken).ConfigureAwait(false);
            }
            else if (!payload.ShouldLockNow
                && sync.Session.Session.State == BillingSessionState.Active
                && (_lockSentForBillingSessions.Remove(sync.Session.Session.Id) || unlockWhenAvailable))
            {
                await _networkServer.SendMachineCommandWithResultAsync(
                    machineId,
                    CommandType.UNLOCK,
                    issuedBy: "Billing",
                    reason: "Customer account balance was topped up.",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        AdminBillingResult result = AdminBillingResult.Success(machineId, message, payload);
        BillingUpdated?.Invoke(result);
        return result;
    }

    // Tao ket qua loi va phat event cho UI.
    private AdminBillingResult Error(string machineId, string errorCode, string message)
    {
        var result = AdminBillingResult.ControlledError(machineId, errorCode, message);
        BillingUpdated?.Invoke(result);
        return result;
    }

    // Chuyen BillingSyncSession thanh TimerPayload de gui qua network cho client.
    private async Task<TimerPayload> ToTimerPayloadAsync(
        BillingSyncSession sync,
        CancellationToken cancellationToken)
    {
        BillingSessionRecord session = sync.Session.Session;
        BillingCalculation calculation = sync.Session.Calculation;
        long elapsedSeconds = Math.Max(0, (long)Math.Floor((calculation.AsOfUtc - session.StartedAtUtc).TotalSeconds));
        BalanceSnapshot? balance = await GetBalanceSnapshotAsync(session, calculation, cancellationToken).ConfigureAwait(false);
        bool isBalanceDepleted = balance?.RemainingUsageSeconds <= 0;
        bool isActive = session.State == BillingSessionState.Active;
        bool isWarning = isActive
            && sync.RemainingSeconds is > 0
            && sync.RemainingSeconds <= (long)WarningWindow.TotalSeconds;

        return new TimerPayload
        {
            MachineId = session.MachineId,
            RentalMode = session.RentalMode.ToString(),
            RemainingSeconds = sync.RemainingSeconds,
            ElapsedSeconds = elapsedSeconds,
            StartedAt = session.StartedAtUtc,
            ExpiresAt = session.ExpiresAtUtc,
            RatePerHour = session.RatePerHour,
            ChargedMinutes = calculation.ChargedMinutes,
            AmountVnd = calculation.AmountVnd,
            RemainingBalanceVnd = balance?.RemainingBalanceVnd,
            TotalBalanceVnd = balance?.TotalBalanceVnd,
            RemainingUsageSeconds = balance?.RemainingUsageSeconds,
            IsWarning = isWarning,
            ShouldLockNow = sync.ShouldLockNow || (isActive && isBalanceDepleted),
            Status = session.State.ToString()
        };
    }

    // Lay thong tin so du cua khach va tinh so giay su dung con lai theo so tien da nap.
    private async Task<BalanceSnapshot?> GetBalanceSnapshotAsync(
        BillingSessionRecord session,
        BillingCalculation calculation,
        CancellationToken cancellationToken)
    {
        if (_customers is null || !TryGetCustomerId(session.UserId, out string customerId))
        {
            return null;
        }

        CustomerRecord? customer = await _customers.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return null;
        }

        long remainingBalance = Math.Max(0, customer.AccountBalance - calculation.AmountVnd);
        long totalPaidSeconds = calculation.RatePerHour <= 0
            ? 0
            : (long)Math.Floor(customer.AccountBalance * 3600.0 / calculation.RatePerHour);
        long elapsedSeconds = Math.Max(0, (long)Math.Floor((calculation.AsOfUtc - session.StartedAtUtc).TotalSeconds));
        long remainingUsageSeconds = Math.Max(0, totalPaidSeconds - elapsedSeconds);
        return new BalanceSnapshot(customer.AccountBalance, remainingBalance, remainingUsageSeconds);
    }

    // Tach customerId tu userId co dang "customer-...".
    private static bool TryGetCustomerId(string userId, out string customerId)
    {
        customerId = string.Empty;
        if (!userId.StartsWith(CustomerAuthUserIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        customerId = userId[CustomerAuthUserIdPrefix.Length..].Trim();
        return customerId.Length > 0;
    }

    // Chuan hoa ma may: null thanh chuoi rong va cat khoang trang.
    private static string NormalizeMachineId(string? machineId)
        => machineId?.Trim() ?? string.Empty;

    // Don gia <= 0 se quay ve don gia mac dinh.
    private static long NormalizeRatePerHour(long ratePerHour)
        => ratePerHour <= 0 ? DefaultRatePerHour : ratePerHour;

    // PC00 la may server nen khong tinh tien nhu may client.
    private static bool IsServerMachine(string? machineId)
        => string.Equals(machineId?.Trim(), ServerMachineId, StringComparison.OrdinalIgnoreCase);

    // Snapshot so du ngan gon dung khi tao TimerPayload.
    private sealed record BalanceSnapshot(long TotalBalanceVnd, long RemainingBalanceVnd, long RemainingUsageSeconds);
}
