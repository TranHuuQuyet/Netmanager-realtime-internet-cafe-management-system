namespace ServerApp.Database.Models;

public enum BillingRentalMode
{
    Timed = 0,
    OpenEnded = 1,
    Extend = 2
}

public enum BillingSessionState
{
    Active = 0,
    Closed = 1
}

public sealed record BillingSessionRecord(
    string Id,
    string AuthSessionId,
    string UserId,
    string Username,
    string MachineId,
    BillingRentalMode RentalMode,
    BillingSessionState State,
    long RatePerHour,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? EndedAtUtc,
    long ChargedMinutes,
    long AmountVnd);
