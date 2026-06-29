using ServerApp.Billing.Contracts;
using ServerApp.Billing.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using ServerApp.Networking;
using Shared.DTOs.CommandPayloads;

namespace ServerApp.Presentation;

public sealed class NetworkAdminBillingService : IAdminBillingService
{
    private const long DefaultRatePerHour = 10_000;
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(5);

    private readonly IBillingService _billing;
    private readonly ISessionRepository _sessions;
    private readonly TcpJsonLineServer? _networkServer;
    private readonly HashSet<string> _lockSentForBillingSessions = new(StringComparer.OrdinalIgnoreCase);

    public NetworkAdminBillingService(
        IBillingService billing,
        ISessionRepository sessions,
        TcpJsonLineServer? networkServer)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _networkServer = networkServer;
    }

    public event Action<AdminBillingResult>? BillingUpdated;

    public async Task<AdminBillingResult> StartTimedAsync(
        string machineId,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
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
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminBillingResult> StartOpenEndedAsync(
        string machineId,
        CancellationToken cancellationToken = default)
    {
        return await OpenAsync(
            NormalizeMachineId(machineId),
            BillingRentalMode.OpenEnded,
            DateTimeOffset.UtcNow,
            null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminBillingResult> ExtendAsync(
        string machineId,
        int extensionMinutes,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
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

    public async Task<AdminBillingResult?> SyncMachineAsync(
        string machineId,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
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
            results.Add(await PublishAsync(session, "Billing session refreshed.", cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    private async Task<AdminBillingResult> OpenAsync(
        string targetMachineId,
        BillingRentalMode mode,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetMachineId))
        {
            return Error(targetMachineId, "INVALID_MACHINE_ID", "Machine ID is required.");
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
                DefaultRatePerHour,
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
        TimerPayload payload = ToTimerPayload(sync);
        string machineId = payload.MachineId;

        if (_networkServer is not null)
        {
            await _networkServer.SendTimerAsync(machineId, payload, cancellationToken).ConfigureAwait(false);

            if (sync.ShouldLockNow
                && sync.Session.Session.State == BillingSessionState.Active
                && _lockSentForBillingSessions.Add(sync.Session.Session.Id))
            {
                await _networkServer.SendMachineCommandWithResultAsync(
                    machineId,
                    lockMachine: true,
                    issuedBy: "Billing",
                    reason: "Timed billing session expired.",
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

    private static TimerPayload ToTimerPayload(BillingSyncSession sync)
    {
        BillingSessionRecord session = sync.Session.Session;
        BillingCalculation calculation = sync.Session.Calculation;
        bool isActive = session.State == BillingSessionState.Active;
        bool isWarning = isActive
            && sync.RemainingSeconds is > 0
            && sync.RemainingSeconds <= (long)WarningWindow.TotalSeconds;

        return new TimerPayload
        {
            MachineId = session.MachineId,
            RentalMode = session.RentalMode.ToString(),
            RemainingSeconds = sync.RemainingSeconds,
            StartedAt = session.StartedAtUtc,
            ExpiresAt = session.ExpiresAtUtc,
            RatePerHour = session.RatePerHour,
            ChargedMinutes = calculation.ChargedMinutes,
            AmountVnd = calculation.AmountVnd,
            IsWarning = isWarning,
            ShouldLockNow = sync.ShouldLockNow,
            Status = session.State.ToString()
        };
    }

    private static string NormalizeMachineId(string? machineId)
        => machineId?.Trim() ?? string.Empty;
}
