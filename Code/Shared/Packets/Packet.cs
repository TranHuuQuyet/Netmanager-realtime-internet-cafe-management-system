using System.Text.Json.Serialization;
using NETManager.Shared.Enums;
using NETManager.Shared.Models;
using NETManager.Shared.Utilities;
using NETManager.Shared.DTOs.Bidrectional;
using NETManager.Shared.DTOs.CommandPayloads;
using NETManager.Shared.DTOs.RequestPayloads;
using NETManager.Shared.DTOs.ResponsePayloads;
namespace NETManager.Shared.Packets;

public abstract class Packet
{
    [JsonPropertyName("type")]
    public PacketType Type { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }

    [JsonIgnore]
    public abstract object Payload { get; }

    protected Packet() { }

    protected Packet(PacketType type, string source, string target, string? requestId = null)
    {
        Type = type;
        Source = source;
        Target = target;
        RequestId = requestId;
        Timestamp = DateTime.UtcNow;
    }
}

public class Packet<T> : Packet where T : class
{
    [JsonPropertyName("payload")]
    public T TypedPayload { get; set; } = default!;

    [JsonIgnore]
    public override object Payload => TypedPayload;

    public Packet() : base() { }

    public Packet(PacketType type, string source, string target, T payload, string? requestId = null)
        : base(type, source, target, requestId)
    {
        TypedPayload = payload;
    }
}
