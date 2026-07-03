namespace ServerApp.Networking;

// Immediate result for an Admin -> Client NOTIFICATION send attempt.
public sealed record MachineNotificationSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);

public sealed record MachineNotificationBroadcastResult(
    bool Sent,
    string Status,
    string Message,
    int TargetCount,
    int SentCount,
    string? ErrorCode = null);
