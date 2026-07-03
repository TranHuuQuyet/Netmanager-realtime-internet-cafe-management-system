// Namespace chua cac model lien quan den tinh tien/thanh toan.
namespace ServerApp.Database.Models;

// Cac kieu thue may.
// Timed: thue theo khoang thoi gian co han.
// OpenEnded: mo may khong dat truoc gio ket thuc.
// Extend: gia han them thoi gian cho phien dang co.
public enum BillingRentalMode
{
    Timed = 0,
    OpenEnded = 1,
    Extend = 2
}

// Trang thai cua phien tinh tien.
// Active la con dang chay, Closed la da dong va da co ket qua tinh tien cuoi.
public enum BillingSessionState
{
    Active = 0,
    Closed = 1
}

// Ban ghi phien tinh tien cho mot lan khach su dung may.
// No lien ket voi session dang nhap, user, may, gia tien, thoi gian va so tien da tinh.
public sealed record BillingSessionRecord(
    string Id,
    string AuthSessionId,
    string UserId,
    string Username,
    string MachineId,
    BillingRentalMode RentalMode,
    BillingSessionState State,
    long RatePerHour,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? EndedAtUtc,
    long ChargedMinutes,
    long AmountVnd);
