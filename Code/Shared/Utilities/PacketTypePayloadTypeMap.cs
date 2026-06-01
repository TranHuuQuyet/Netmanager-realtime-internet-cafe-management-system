using System.Collections.Concurrent;
using Shared.Enums;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.DTOs.Bidrectional;

namespace Shared.Utilities;

public static class PacketTypePayloadTypeMap
{
    private static readonly ConcurrentDictionary<PacketType, Type> _map = new()
    {
        [PacketType.LOGIN] = typeof(LoginPayload),
        [PacketType.STATUS] = typeof(StatusPayload),
        [PacketType.LOCK] = typeof(LockPayload),
        [PacketType.UNLOCK] = typeof(UnlockPayload),
        [PacketType.ACK] = typeof(AckPayload),
        [PacketType.NOTIFICATION] = typeof(NotificationPayload),
        [PacketType.TIMER] = typeof(TimerPayload),
        [PacketType.CHAT] = typeof(ChatPayload)
    };

    /// <summary>Return the payload <see cref="System.Type"/> registered for the given <paramref name="type"/>.</summary>
    public static Type GetPayloadType(PacketType type)
    {
        return _map.TryGetValue(type, out var t)
            ? t
            : throw new NotSupportedException($"No payload type registered for packet type '{type}'.");
    }

    /// <summary>Try to get the payload <see cref="Type"/> for <paramref name="type"/> without throwing.</summary>
    public static bool TryGetPayloadType(PacketType type, out Type? payloadType)
    {
        return _map.TryGetValue(type, out payloadType);
    }

    /// <summary>Return the strongly-typed <see cref="PacketType"/> mapping entry.</summary>
    public static IReadOnlyDictionary<PacketType, Type> Mappings => _map;
}
