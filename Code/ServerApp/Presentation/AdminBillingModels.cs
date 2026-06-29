using Shared.DTOs.CommandPayloads;

namespace ServerApp.Presentation;

public sealed record AdminBillingResult(
    bool IsSuccess,
    string MachineId,
    string Status,
    string Message,
    TimerPayload? Timer,
    string? ErrorCode = null)
{
    public bool IsError => !IsSuccess;

    public static AdminBillingResult Success(string machineId, string message, TimerPayload timer)
        => new(true, machineId, "Success", message, timer);

    public static AdminBillingResult ControlledError(
        string machineId,
        string errorCode,
        string message,
        TimerPayload? timer = null)
        => new(false, machineId, "Error", message, timer, errorCode);
}
