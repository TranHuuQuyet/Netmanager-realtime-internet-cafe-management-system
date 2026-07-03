using Shared.Enums;

// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Ket qua gui lenh tu admin den client.
public sealed record AdminCommandResult(
    string MachineId,
    CommandType Command,
    string Status,
    string Message,
    bool IsError,
    string? ErrorCode = null,
    string? RequestId = null)
{
    // Tao ket qua khi lenh da duoc server chap nhan va gui di.
    public static AdminCommandResult Submitted(
        AdminCommandRequest request,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            request.Command,
            "Submitted",
            message,
            IsError: false,
            RequestId: requestId);

    // Tao ket qua khi server nhan ACK tu client.
    public static AdminCommandResult Ack(
        string machineId,
        CommandType command,
        string status,
        string message,
        string? requestId = null)
        => new(
            machineId,
            command,
            status,
            message,
            IsError: false,
            RequestId: requestId);

    // Tao ket qua loi co kiem soat, vi du request sai hoac client khong san sang.
    public static AdminCommandResult ControlledError(
        AdminCommandRequest request,
        string errorCode,
        string message,
        string? requestId = null)
        => new(
            request.MachineId,
            request.Command,
            "Error",
            message,
            IsError: true,
            ErrorCode: errorCode,
            RequestId: requestId);
}
