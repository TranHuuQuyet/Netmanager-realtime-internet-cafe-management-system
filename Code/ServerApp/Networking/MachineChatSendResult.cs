namespace ServerApp.Networking;

// Immediate result for an Admin -> Client CHAT send attempt.
public sealed record MachineChatSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);
