using ServerApp.Database.Models;

namespace ServerApp.Billing.Models;

public sealed record BillingSessionRequest(
    string AuthSessionId,
    string UserId,
    string Username,
    string MachineId,
    BillingRentalMode RentalMode,
    DateTimeOffset StartedAtUtc,
    long RatePerHour = 10_000,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record BillingCalculation(
    long ChargedMinutes,
    long RatePerHour,
    long AmountVnd,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset AsOfUtc);

public sealed record BillingSessionView(
    BillingSessionRecord Session,
    BillingCalculation Calculation);

public sealed record BillingResult
{
    public required bool IsSuccess { get; init; }

    public required string Message { get; init; }

    public BillingSessionView? Session { get; init; }

    public string? ErrorCode { get; init; }

    public bool IsFailure => !IsSuccess;

    public static BillingResult Success(
        BillingSessionView session,
        string message = "Billing session accepted.")
        => new()
        {
            IsSuccess = true,
            Message = message,
            Session = session
        };

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

public static class BillingCalculator
{
    public static BillingCalculation Calculate(
        DateTimeOffset startedAtUtc,
        DateTimeOffset asOfUtc,
        long ratePerHour = 10_000)
    {
        if (asOfUtc < startedAtUtc)
        {
            asOfUtc = startedAtUtc;
        }

        double elapsedSeconds = (asOfUtc - startedAtUtc).TotalSeconds;
        long chargedMinutes = (long)Math.Ceiling(elapsedSeconds / 60.0);
        long amountVnd = (long)Math.Ceiling(chargedMinutes * ratePerHour / 60.0);

        return new BillingCalculation(
            chargedMinutes,
            ratePerHour,
            amountVnd,
            startedAtUtc,
            asOfUtc);
    }
}
