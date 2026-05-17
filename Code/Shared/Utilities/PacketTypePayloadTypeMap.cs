using System.Collections.Concurrent;
using NETManager.Shared.Enums;

namespace NETManager.Shared.Utilities;

/// <summary>
/// Central lookup that maps a <see cref="PacketType"/> value to the concrete
/// payload <see cref="System.Type"/> that should be used when constructing a
/// <see cref="Packet{T}"/> for that packet type.
///
/// This is the only place the router needs to know about type relationships;
/// the rest of the serializer and the dispatcher code never contains switch/if
/// chains over packet types.
///
/// Usage in the two-pass deserializer:
///   type  = JsonHelper.DeserializePacketType(rawBytes)
///   t     = PacketTypePayloadTypeMap.GetPayloadType(type)
///   packet = JsonHelper.Deserialize<Packet<T>>(rawBytes)
/// </summary>
public static class PacketTypePayloadTypeMap
{
    private static readonly ConcurrentDictionary<PacketType, Type> _map = new()
    {
        [PacketType.LOGIN]        = typeof(LoginPayload),
        [PacketType.STATUS]       = typeof(StatusPayload),
        [PacketType.LOCK]         = typeof(LockPayload),
        [PacketType.UNLOCK]       = typeof(UnlockPayload),
        [PacketType.ACK]          = typeof(AckPayload),
        [PacketType.NOTIFICATION] = typeof(NotificationPayload),
        [PacketType.TIMER]        = typeof(TimerPayload),
        [PacketType.CHAT]         = typeof(ChatPayload)
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
