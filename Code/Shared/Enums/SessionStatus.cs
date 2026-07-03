// Namespace chua cac enum dung chung giua server va client.
namespace Shared.Enums;

// Trang thai tong quat cua mot session.
public enum SessionStatus
{
    // Session dang hoat dong.
    ACTIVE,

    // Session khong hoat dong.
    INACTIVE,

    // Session da het han.
    EXPIRED,

    // Session da bi ket thuc.
    TERMINATED
}
