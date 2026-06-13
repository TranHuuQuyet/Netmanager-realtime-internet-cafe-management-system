using Shared.Enums;

namespace ServerApp.Presentation;

public sealed record AdminCommandResult(
    string MachineId,
    CommandType Command,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    public static AdminCommandResult Submitted(
        AdminCommandRequest request,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            request.Command,
            "Submitted",
            message,
            IsError: false,
            RequestId: requestId);

    public static AdminCommandResult Ack(
        string machineId,
        CommandType command,
        string status,
        string message,
        string? requestId = null)
        => new(
            machineId,
            command,
            status,
            message,
            IsError: false,
            RequestId: requestId);

    public static AdminCommandResult ControlledError(
        AdminCommandRequest request,
        string errorCode,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            request.Command,
            "Error",
            message,
            IsError: true,
            ErrorCode: errorCode,
            RequestId: requestId);
}
