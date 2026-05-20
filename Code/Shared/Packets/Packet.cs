using System.Text.Json.Serialization;

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

    [JsonPropertyName("payload")]
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
    public new T Payload { get; set; } = default!;

    public Packet() : base() { }

    public Packet(PacketType type, string source, string target, T payload, string? requestId = null)
        : base(type, source, target, requestId)
    {
        Payload = payload;
    }
}
