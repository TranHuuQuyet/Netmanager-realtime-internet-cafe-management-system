using System.Text.Json;
using Shared.Enums;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Packets;
using System.IO;

namespace Shared.Utilities.JsonHelper;

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
        using var doc = JsonDocument.Parse(json);
        PacketType type = ReadPacketType(doc);
        bool? success = ReadSuccess(doc);

        Type payloadType = ResolvePayloadType(type, success);
        Type packetType = typeof(Packet<>).MakeGenericType(payloadType);

        return JsonSerializer.Deserialize(json, packetType, JsonSerializerOptions.Shared)
            ?? throw new InvalidDataException("Invalid JSON payload.");
    }

    private static PacketType ReadPacketType(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("type", out var typeElement))
        {
            throw new InvalidDataException("Packet missing required field: type");
        }

        return typeElement.Deserialize<PacketType>(JsonSerializerOptions.Shared);
    }

    private static bool? ReadSuccess(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("success", out var successElement))
        {
            return null;
        }

        if (successElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (successElement.ValueKind != JsonValueKind.True
            && successElement.ValueKind != JsonValueKind.False)
        {
            throw new InvalidDataException("Packet field 'success' must be boolean or null.");
        }

        return successElement.GetBoolean();
    }

    // LOGIN uses the same packet type for request and response, so the envelope decides the payload.
    private static Type ResolvePayloadType(PacketType type, bool? success)
    {
        if (type == PacketType.LOGIN)
        {
            return success switch
            {
                null => typeof(LoginPayload),
                true => typeof(LoginResultPayload),
                false => typeof(EmptyPayload),
            };
        }

        return type switch
        {
            PacketType.STATUS => typeof(StatusPayload),
            PacketType.LOCK => typeof(LockPayload),
            PacketType.UNLOCK => typeof(UnlockPayload),
            PacketType.ACK => typeof(AckPayload),
            PacketType.NOTIFICATION => typeof(NotificationPayload),
            PacketType.TIMER => typeof(TimerPayload),
            PacketType.CHAT => typeof(ChatPayload),
            _ => throw new InvalidDataException($"Unsupported packet type: {type}")
        };
    }
}
