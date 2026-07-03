using ServerApp.Auth.Models;

// Namespace chua cac model du lieu dung trong tang Database.
// Cac record o day thuong la DTO/record gon nhe de truyen du lieu giua cac lop.
namespace ServerApp.Database.Models;

// Luu ket qua bam mat khau.
// SaltBase64 la chuoi salt da ma hoa Base64.
// HashBase64 la chuoi hash mat khau da ma hoa Base64.
public sealed record PasswordHash(string SaltBase64, string HashBase64);

// Tai khoan mau dung de seed du lieu ban dau.
// Role xac dinh quyen cua user, IsActive cho biet tai khoan co duoc phep dang nhap khong.
public sealed record SeedAccount(string Username, string Password, string MachineId, UserRole Role, bool IsActive = true);

// Ban ghi user day du hon, thuong dung khi doc/ghi user tu database hoac file luu tru.
// Record giup tao doi tuong chi de mang du lieu, it logic, de so sanh va copy bang "with".
public sealed record UserRecord(
    string Id,
    string Username,
    string PasswordSaltBase64,
    string PasswordHashBase64,
    UserRole Role,
    string? MachineId,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);

// Ban ghi session dang nhap cua user.
// No noi user nao dang/da dung may nao, trang thai session ra sao va thoi gian bat dau/ket thuc.
public sealed record SessionRecord(
    string Id,
    string UserId,
    string Username,
    UserRole Role,
    string? MachineId,
    SessionState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

// Ban ghi thong tin khach hang.
// Dung cho nghiep vu quan ly tai khoan khach, thong tin ca nhan va so du tai khoan.
public sealed record CustomerRecord(
    string CustomerId,
    string FirstName,
    string LastName,
    string Phone,
    string IdentityNumber,
    string Birthday,
    string Username,
    string Password,
    long AccountBalance);
