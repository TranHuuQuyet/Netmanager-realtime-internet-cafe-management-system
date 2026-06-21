using ServerApp.Billing.Contracts;
using ServerApp.Billing.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;

namespace ServerApp.Billing.Services;

public sealed class BillingService : IBillingService
{
    private readonly IBillingSessionRepository _billingSessions;

    public BillingService(IBillingSessionRepository billingSessions)
    {
        _billingSessions = billingSessions;
    }

    public BillingCalculation CalculateAmount(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000)
        => BillingCalculator.Calculate(startedAtUtc, asOfUtc, ratePerHour);

    public async Task<BillingResult> OpenSessionAsync(
        BillingSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AuthSessionId)
            || string.IsNullOrWhiteSpace(request.UserId)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.MachineId))
        {
            return BillingResult.Failure(
                "INVALID_BILLING_SESSION",
                "Billing session requires auth session, user and machine metadata.");
        }

        var existing = await _billingSessions.GetActiveByMachineIdAsync(request.MachineId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return BillingResult.Failure(
                "BILLING_SESSION_ALREADY_ACTIVE",
                "Machine already has an active billing session.");
        }

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

        await _billingSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);

        var calculation = CalculateAmount(session.StartedAtUtc, session.StartedAtUtc, session.RatePerHour);
        return BillingResult.Success(
            new BillingSessionView(session, calculation),
            "Billing session opened.");
    }

    public async Task<BillingResult> ExtendSessionAsync(
        string billingSessionId,
        TimeSpan extension,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var session = await _billingSessions.GetByIdAsync(billingSessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return BillingResult.Failure("BILLING_SESSION_NOT_FOUND", "Billing session was not found.");
        }

        var updatedSession = session with
        {
            RentalMode = BillingRentalMode.Extend,
            ExpiresAtUtc = session.ExpiresAtUtc is null
                ? asOfUtc.Add(extension)
                : session.ExpiresAtUtc.Value.Add(extension),
            ChargedMinutes = CalculateAmount(session.StartedAtUtc, asOfUtc, session.RatePerHour).ChargedMinutes,
            AmountVnd = CalculateAmount(session.StartedAtUtc, asOfUtc, session.RatePerHour).AmountVnd
        };

        await _billingSessions.UpdateAsync(updatedSession, cancellationToken).ConfigureAwait(false);

        return BillingResult.Success(
            new BillingSessionView(
                updatedSession,
                CalculateAmount(updatedSession.StartedAtUtc, asOfUtc, updatedSession.RatePerHour)),
            "Billing session extended.");
    }

    public async Task<BillingResult> CloseSessionAsync(
        string billingSessionId,
        DateTimeOffset endedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var session = await _billingSessions.GetByIdAsync(billingSessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return BillingResult.Failure("BILLING_SESSION_NOT_FOUND", "Billing session was not found.");
        }

        var calculation = CalculateAmount(session.StartedAtUtc, endedAtUtc, session.RatePerHour);
        await _billingSessions.CloseAsync(
                session.Id,
                endedAtUtc,
                calculation.ChargedMinutes,
                calculation.AmountVnd,
                cancellationToken)
            .ConfigureAwait(false);

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
}
