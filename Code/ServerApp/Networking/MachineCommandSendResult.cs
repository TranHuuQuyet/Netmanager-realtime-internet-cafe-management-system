namespace ServerApp.Networking;

public sealed record MachineCommandSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);
