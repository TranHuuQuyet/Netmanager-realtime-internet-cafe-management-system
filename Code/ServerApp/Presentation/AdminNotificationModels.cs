// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Yeu cau gui thong bao tu admin den mot may hoac mot pham vi may.
public sealed record AdminNotificationRequest(
    string MachineId,
    string Message,
    string Severity = "Info",
    string Scope = "Direct");

// Ket qua gui thong bao.
public sealed record AdminNotificationResult(
    string MachineId,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    // Tao ket qua gui thanh cong.
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

    // Tao ket qua loi co kiem soat.
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
