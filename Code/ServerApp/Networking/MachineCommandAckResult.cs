using Shared.Enums;

namespace ServerApp.Networking;

// Kết quả cuối của command sau khi server xử lý ACK từ client.
//
// Flow:
// - Admin gửi LOCK/UNLOCK, server lưu pending command theo requestId.
// - Client trả ACK kèm requestId + machineId + ackFor + status.
// - Server validate ACK rồi emit record này cho tầng M3/UI.
//
// Có thể là success hoặc error:
// - ACK hợp lệ: status Success/Failed/Ignored
// - ACK sai requestId, sai máy, sai type, client disconnect, timeout
public sealed record MachineCommandAckResult(
    string MachineId,
    CommandType Command,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode,
    string RequestId);
