using Shared.Enums;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.DTOs.Bidrectional;
using Shared.Models;

// Namespace chua cau truc packet dung cho giao tiep network.
namespace Shared.Packets;

// Factory gom cac ham tao packet dung type/payload tuong ung.
public static class PacketFactory
{
    // Tao packet LOGIN request.
    public static Packet<LoginPayload> CreateLogin(
        string source, string target, LoginPayload payload, string? requestId = null)
    {
        return new Packet<LoginPayload>(PacketType.LOGIN, source, target, payload, requestId);
    }


    // Tao packet STATUS.
    public static Packet<StatusPayload> CreateStatus(
        string source, string target, StatusPayload payload, string? requestId = null)
    {
        return new Packet<StatusPayload>(PacketType.STATUS, source, target, payload, requestId);
    }

    // Tao packet LOCK.
    public static Packet<LockPayload> CreateLock(
        string source, string target, LockPayload payload, string? requestId = null)
    {
        return new Packet<LockPayload>(PacketType.LOCK, source, target, payload, requestId);
    }

    // Tao packet UNLOCK.
    public static Packet<UnlockPayload> CreateUnlock(
        string source, string target, UnlockPayload payload, string? requestId = null)
    {
        return new Packet<UnlockPayload>(PacketType.UNLOCK, source, target, payload, requestId);
    }

    // Tao packet SHUTDOWN.
    public static Packet<ShutdownPayload> CreateShutdown(
        string source, string target, ShutdownPayload payload, string? requestId = null)
    {
        return new Packet<ShutdownPayload>(PacketType.SHUTDOWN, source, target, payload, requestId);
    }

    // Tao packet ACK.
    public static Packet<AckPayload> CreateAck(
        string source, string target, AckPayload payload, string? requestId = null)
    {
        return new Packet<AckPayload>(PacketType.ACK, source, target, payload, requestId);
    }

    // Tao packet NOTIFICATION.
    public static Packet<NotificationPayload> CreateNotification(
        string source, string target, NotificationPayload payload, string? requestId = null)
    {
        return new Packet<NotificationPayload>(PacketType.NOTIFICATION, source, target, payload, requestId);
    }

    // Tao packet TIMER.
    public static Packet<TimerPayload> CreateTimer(
        string source, string target, TimerPayload payload, string? requestId = null)
    {
        return new Packet<TimerPayload>(PacketType.TIMER, source, target, payload, requestId);
    }

    // Tao packet CHAT.
    public static Packet<ChatPayload> CreateChat(
        string source, string target, ChatPayload payload, string? requestId = null)
    {
        return new Packet<ChatPayload>(PacketType.CHAT, source, target, payload, requestId);
    }

    // Tao packet LOGIN response thanh cong.
    public static Packet<LoginResultPayload> CreateLoginSuccess(
        string source, string target, LoginResultPayload payload, string? requestId = null, string? message = "Login accepted")
    {
        return new Packet<LoginResultPayload>(PacketType.LOGIN, source, target, payload, requestId)
        {
            Success = true,
            Message = message
        };
    }

    // Tao packet LOGIN response that bai.
    public static Packet<EmptyPayload> CreateLoginFailed(
        string source, string target, string errorCode, string? details = null, string? requestId = null, string? message = "Login rejected")
    {
        return new Packet<EmptyPayload>(PacketType.LOGIN, source, target, new EmptyPayload(), requestId)
        {
            Success = false,
            Message = message,
            Error = new ErrorInfo
            {
                Code = errorCode,
                Details = details
            }
        };
    }
}
