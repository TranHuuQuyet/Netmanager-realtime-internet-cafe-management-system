// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Yeu cau admin gui tin nhan den mot may.
public sealed record AdminChatRequest(
    string MachineId,
    string Message);

// Tin nhan chat da nhan/gui trong man hinh admin.
public sealed record AdminChatMessage(
    string MachineId,
    string Sender,
    string Message,
    DateTimeOffset Timestamp);

// Ket qua gui chat tu admin den client.
public sealed record AdminChatResult(
    string MachineId,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    // Tao ket qua da gui thanh cong den tang network.
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

    // Tao ket qua loi co kiem soat, vi du may offline hoac message khong hop le.
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
