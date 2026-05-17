using NETManager.Shared.Enums;
using NETManager.Shared.DTOs.RequestPayloads;
using NETManager.Shared.DTOs.CommandPayloads;
using NETManager.Shared.DTOs.ResponsePayloads;
using NETManager.Shared.DTOs.Bidrectional;

namespace NETManager.Shared.Packets;

public static class PacketFactory
{
    public static Packet<LoginPayload> CreateLogin(
        string source, string target, LoginPayload payload, string? requestId = null)
    {
        return new Packet<LoginPayload>(PacketType.LOGIN, source, target, payload, requestId);
    }

    public static Packet<StatusPayload> CreateStatus(
        string source, string target, StatusPayload payload, string? requestId = null)
    {
        return new Packet<StatusPayload>(PacketType.STATUS, source, target, payload, requestId);
    }

    public static Packet<LockPayload> CreateLock(
        string source, string target, LockPayload payload, string? requestId = null)
    {
        return new Packet<LockPayload>(PacketType.LOCK, source, target, payload, requestId);
    }

    public static Packet<UnlockPayload> CreateUnlock(
        string source, string target, UnlockPayload payload, string? requestId = null)
    {
        return new Packet<UnlockPayload>(PacketType.UNLOCK, source, target, payload, requestId);
    }

    public static Packet<AckPayload> CreateAck(
        string source, string target, AckPayload payload, string? requestId = null)
    {
        return new Packet<AckPayload>(PacketType.ACK, source, target, payload, requestId);
    }

    public static Packet<NotificationPayload> CreateNotification(
        string source, string target, NotificationPayload payload, string? requestId = null)
    {
        return new Packet<NotificationPayload>(PacketType.NOTIFICATION, source, target, payload, requestId);
    }

    public static Packet<TimerPayload> CreateTimer(
        string source, string target, TimerPayload payload, string? requestId = null)
    {
        return new Packet<TimerPayload>(PacketType.TIMER, source, target, payload, requestId);
    }

    public static Packet<ChatPayload> CreateChat(
        string source, string target, ChatPayload payload, string? requestId = null)
    {
        return new Packet<ChatPayload>(PacketType.CHAT, source, target, payload, requestId);
    }

    public static Packet<LoginResultPayload> CreateLoginSuccess(
        string source, string target, LoginResultPayload payload, string? requestId = null)
    {
        return new Packet<LoginResultPayload>(PacketType.LOGIN, source, target, payload, requestId);
    }

    public static Packet<LoginFailedPayload> CreateLoginFailed(
        string source, string target, LoginFailedPayload payload, string? requestId = null)
    {
        return new Packet<LoginFailedPayload>(PacketType.LOGIN, source, target, payload, requestId);
    }
}
