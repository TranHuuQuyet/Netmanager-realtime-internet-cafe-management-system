namespace ServerApp.Presentation;

public sealed record AdminChatRequest(
    string MachineId,
    string Message);

public sealed record AdminChatMessage(
    string MachineId,
    string Sender,
    string Message,
    DateTimeOffset Timestamp);

public sealed record AdminChatResult(
    string MachineId,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    public static AdminChatResult Sent(
        AdminChatRequest request,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            "Sent",
            message,
            IsError: false,
            RequestId: requestId);

    public static AdminChatResult ControlledError(
        AdminChatRequest request,
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
