// Namespace chua cac enum dung chung giua server va client.
namespace Shared.Enums;

// Cac loai lenh dieu khien server co the gui den client.
public enum CommandType
{
    // Khoa may client.
    LOCK,

    // Mo khoa may client.
    UNLOCK,

    // Tat may/ung dung client.
    SHUTDOWN,

    // Gui thong bao.
    NOTIFY,

    // Cap nhat timer billing.
    UPDATE_TIMER,

    // Gui tin nhan chat.
    CHAT
}
