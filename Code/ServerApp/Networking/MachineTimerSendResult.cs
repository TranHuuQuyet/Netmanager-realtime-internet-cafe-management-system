namespace ServerApp.Networking;

// Immediate result for a Server -> Client TIMER billing sync attempt.
public sealed record MachineTimerSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);
