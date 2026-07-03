namespace ServerApp.Presentation;

public sealed record AdminNotificationRequest(
    string MachineId,
    string Message,
    string Severity = "Info",
    string Scope = "Direct");

public sealed record AdminNotificationResult(
    string MachineId,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    public static AdminNotificationResult Sent(
        AdminNotificationRequest request,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            "Sent",
            message,
            IsError: false,
            RequestId: requestId);

    public static AdminNotificationResult ControlledError(
        AdminNotificationRequest request,
        string errorCode,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            "Error",
            message,
            IsError: true,
            ErrorCode: errorCode,
            RequestId: requestId);
}
