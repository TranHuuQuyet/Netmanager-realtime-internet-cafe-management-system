// Namespace chua cac enum dung chung giua server va client.
namespace Shared.Enums;

// Loai goi tin trong giao thuc JSON line.
public enum PacketType
{
    // Dang nhap.
    LOGIN,

    // Bao trang thai may.
    STATUS,

    // Khoa may.
    LOCK,

    // Mo khoa may.
    UNLOCK,

    // Tat may/ung dung.
    SHUTDOWN,

    // Phan hoi da nhan/xu ly command.
    ACK,

    // Thong bao tu server/admin.
    NOTIFICATION,

    // Dong bo timer billing.
    TIMER,

    // Chat hai chieu.
    CHAT
}
