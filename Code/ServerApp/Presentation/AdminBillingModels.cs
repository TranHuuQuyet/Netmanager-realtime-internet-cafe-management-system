using Shared.DTOs.CommandPayloads;

// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Ket qua mot thao tac billing tu UI admin.
public sealed record AdminBillingResult(
    bool IsSuccess,
    string MachineId,
    string Status,
    string Message,
    TimerPayload? Timer,
    string? ErrorCode = null)
{
    // Tien ich de UI kiem tra nhanh ket qua loi.
    public bool IsError => !IsSuccess;

    // Tao ket qua thanh cong kem timer de client hien thi.
    public static AdminBillingResult Success(string machineId, string message, TimerPayload timer)
        => new(true, machineId, "Success", message, timer);

    // Tao ket qua loi da du doan/kiem soat duoc.
    public static AdminBillingResult ControlledError(
        string machineId,
        string errorCode,
        string message,
        TimerPayload? timer = null)
        => new(false, machineId, "Error", message, timer, errorCode);
}
