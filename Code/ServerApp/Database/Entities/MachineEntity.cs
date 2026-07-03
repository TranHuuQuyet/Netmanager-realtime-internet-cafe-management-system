// Namespace chua cac entity dai dien cho du lieu luu trong database.
namespace ServerApp.Database.Entities;

// Entity dai dien cho mot may tram trong quan net.
public sealed record MachineEntity
{
    // Ma dinh danh duy nhat trong he thong/database.
    public Guid Id { get; init; }

    // Ma may de nguoi dung/he thong nhan dien, vi du "PC01".
    public string MachineId { get; init; } = string.Empty;

    // Ten hien thi cua may.
    public string MachineName { get; init; } = string.Empty;

    // Dia chi IP cua may, co the null neu chua biet hoac may chua ket noi.
    public string? IpAddress { get; init; }

    // Trang thai hien tai cua may, vi du Online, Offline, Busy.
    public string Status { get; init; } = string.Empty;

    // Lan cuoi server nhan duoc tin hieu/ket noi tu may.
    public DateTime? LastSeen { get; init; }

    // Cho biet may con duoc quan ly/su dung trong he thong hay khong.
    public bool IsActive { get; init; }
}
