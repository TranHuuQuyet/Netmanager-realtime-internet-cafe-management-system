using ServerApp.Billing.Contracts;
using ServerApp.Billing.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using ServerApp.Networking;
using Shared.Enums;
using Shared.DTOs.CommandPayloads;

namespace ServerApp.Presentation;

public sealed class NetworkAdminBillingService : IAdminBillingService
{
    private const long DefaultRatePerHour = 10_000;
    private const string ServerMachineId = "PC00";
    private const string CustomerAuthUserIdPrefix = "customer-";
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(5);

    private readonly IBillingService _billing;
    private readonly ISessionRepository _sessions;
    private readonly TcpJsonLineServer? _networkServer;
    private readonly ICustomerRepository? _customers;
    private readonly HashSet<string> _lockSentForBillingSessions = new(StringComparer.OrdinalIgnoreCase);

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

    public event Action<AdminBillingResult>? BillingUpdated;

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
            cancellationToken).ConfigureAwait(false);
    }

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

    private async Task<AdminBillingResult> PublishAsync(
        BillingSyncSession sync,
        string message,
        CancellationToken cancellationToken)
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
                && _lockSentForBillingSessions.Remove(sync.Session.Session.Id))
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

    private AdminBillingResult Error(string machineId, string errorCode, string message)
    {
        var result = AdminBillingResult.ControlledError(machineId, errorCode, message);
        BillingUpdated?.Invoke(result);
        return result;
    }

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

    private static string NormalizeMachineId(string? machineId)
        => machineId?.Trim() ?? string.Empty;

    private static long NormalizeRatePerHour(long ratePerHour)
        => ratePerHour <= 0 ? DefaultRatePerHour : ratePerHour;

    private static bool IsServerMachine(string? machineId)
        => string.Equals(machineId?.Trim(), ServerMachineId, StringComparison.OrdinalIgnoreCase);

    private sealed record BalanceSnapshot(long TotalBalanceVnd, long RemainingBalanceVnd, long RemainingUsageSeconds);
}
