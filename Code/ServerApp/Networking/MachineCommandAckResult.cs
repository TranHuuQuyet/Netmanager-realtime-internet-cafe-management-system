using Shared.Enums;

namespace ServerApp.Networking;

public sealed record MachineCommandAckResult(
    string MachineId,
    CommandType Command,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode,
    string RequestId);
