namespace ServerApp.Networking;

// Kết quả trả về ngay sau khi admin bấm gửi lệnh LOCK/UNLOCK.
//
// Giai đoạn này CHƯA phải ACK từ client.
// Nó chỉ trả lời câu hỏi: server có gửi được command xuống client không?
//
// Thường trả lỗi khi:
// - machineId rỗng hoặc sai
// - máy chưa online / chưa bind với TCP connection
// - session/machine không được phép nhận command
// - socket lỗi khi đang send
public sealed record MachineCommandSendResult(
    bool Sent,
    string Status,
    string Message,
    string? ErrorCode = null,
    string? RequestId = null);
