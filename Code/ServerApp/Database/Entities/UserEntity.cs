// Namespace chua cac entity dai dien cho du lieu luu trong database.
namespace ServerApp.Database.Entities;

// Entity dai dien cho mot nguoi dung trong he thong.
// Record co loi khi can tao ban sao moi bang "with" thay vi sua truc tiep doi tuong cu.
public sealed record UserEntity
{
    // Ma dinh danh duy nhat cua user.
    public Guid Id { get; init; }

    // Ten dang nhap cua user.
    public string Username { get; init; } = string.Empty;

    // Mat khau hoac gia tri mat khau da xu ly tuy cach he thong dang luu.
    public string Password { get; init; } = string.Empty;

    // Vai tro/quyen cua user, vi du Admin hoac Customer.
    public string Role { get; init; } = string.Empty;

    // Ma may gan voi user neu tai khoan nay bi rang buoc voi mot may cu the.
    public string? MachineId { get; init; }

    // Cho biet tai khoan con hoat dong hay da bi vo hieu hoa.
    public bool IsActive { get; init; }

    // Thoi diem dang nhap gan nhat; null neu chua tung dang nhap.
    public DateTime? LastLogin { get; init; }

    // Thoi diem tao tai khoan.
    public DateTime CreatedAt { get; init; }
}
