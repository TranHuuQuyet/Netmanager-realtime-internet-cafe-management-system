// using System.Text.Json;
// using System.Text.Json.Serialization;
// using NETManager.Shared.Enums;
// using NETManager.Shared.Models;

// namespace NETManager.Shared.Utilities;

// public static class JsonHelper
// {
//     public static byte[] Serialize(object obj)
//     {
//         return JsonSerializer.Serialize(obj, obj.GetType(), Shared);
//     }

//     public static T Deserialize<T>(byte[] data)
//     {
//         return JsonSerializer.Deserialize<T>(data, Shared)!;
//     }

//     public static PacketType DeserializePacketType(byte[] data)
//     {
//         using var doc = JsonDocument.Parse(data);
//         if (doc.RootElement.TryGetProperty("type", out var typeElement))
//         {
//             return typeElement.Deserialize<PacketType>(Shared);
//         }
//         throw new InvalidDataException("Packet missing required field: type");
//     }
// }
using System.Text.Json;
using NETManager.Shared.Enums;
using NETManager.Shared.Models;
using NETManager.Shared.DTOs.Bidrectional;
using NETManager.Shared.DTOs.CommandPayloads;
using NETManager.Shared.DTOs.RequestPayloads;
using NETManager.Shared.Packets;
using System.IO;
using System.Text.Json.Serialization;


namespace NETManager.Shared.Utilities.JsonHelper;

public static class JsonHelper
{
    public static string SerializeToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, obj.GetType(), JsonSerializerOptions.Shared);
    }

    public static T DeserializeFromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Shared)
            ?? throw new InvalidDataException("Invalid JSON payload.");
    }

    public static PacketType DeserializePacketType(string json)
    {
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("type", out var typeElement))
        {
            throw new InvalidDataException("Packet missing required field: type");
        }

        return typeElement.Deserialize<PacketType>(JsonSerializerOptions.Shared);
    }

    public static object DeserializePacket(string json)
    {
        var type = DeserializePacketType(json);

        return type switch
        {
            PacketType.LOGIN => DeserializeFromJson<Packet<LoginPayload>>(json),
            PacketType.STATUS => DeserializeFromJson<Packet<StatusPayload>>(json),
            PacketType.LOCK => DeserializeFromJson<Packet<LockPayload>>(json),
            PacketType.UNLOCK => DeserializeFromJson<Packet<UnlockPayload>>(json),
            PacketType.ACK => DeserializeFromJson<Packet<AckPayload>>(json),
            PacketType.NOTIFICATION => DeserializeFromJson<Packet<NotificationPayload>>(json),
            PacketType.TIMER => DeserializeFromJson<Packet<TimerPayload>>(json),
            PacketType.CHAT => DeserializeFromJson<Packet<ChatPayload>>(json),
            _ => throw new InvalidDataException($"Unsupported packet type: {type}")
        };
    }
}
