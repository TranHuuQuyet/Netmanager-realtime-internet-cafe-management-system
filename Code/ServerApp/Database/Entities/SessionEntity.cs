// Namespace chua cac entity dai dien cho du lieu luu trong database.
namespace ServerApp.Database.Entities;

// Entity dai dien cho mot phien su dung may cua user.
// Mot session bat dau khi user duoc cap/may duoc mo va ket thuc khi user ngung su dung.
public sealed record SessionEntity
{
    // Ma dinh danh duy nhat cua session.
    public Guid Id { get; init; }

    // User dang so huu session nay.
    public Guid UserId { get; init; }

    // Ma may duoc user su dung trong session.
    public string MachineId { get; init; } =
        string.Empty;

    // Trang thai session, vi du "Active" hoac "Ended".
    public string Status { get; init; } =
        string.Empty;

    // Thoi diem session bat dau.
    public DateTime StartedAt { get; init; }

    // Thoi diem session ket thuc; null khi session con dang chay.
    public DateTime? EndedAt { get; init; }

    // Thoi diem gan nhat session co hoat dong/duoc cap nhat.
    public DateTime? LastSeen { get; init; }
}
