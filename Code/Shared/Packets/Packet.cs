using System.Text.Json.Serialization;
using Shared.Enums;
using Shared.Models;
using Shared.Utilities;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;

// Namespace chua cau truc packet dung cho giao tiep network.
namespace Shared.Packets;

// Lop packet nen, chua cac truong chung cua moi goi tin.
public abstract class Packet
{
    // Loai goi tin.
    [JsonPropertyName("type")]
    public PacketType Type { get; set; }

    // Noi gui packet.
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    // Noi nhan packet.
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    // Id de noi request voi response/ACK.
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    // Thoi diem tao packet.
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    // Ket qua thanh cong/that bai neu packet la response.
    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    // Thong bao mo ta ket qua.
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    // Thong tin loi neu co.
    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }

    // Payload dang object de code co the doc chung, khong serialize truc tiep field nay.
    [JsonIgnore]
    public abstract object Payload { get; }

    // Constructor rong can cho JSON deserialize.
    protected Packet() { }

    // Constructor tao packet day du cac thong tin chung.
    protected Packet(PacketType type, string source, string target, string? requestId = null)
    {
        Type = type;
        Source = source;
        Target = target;
        RequestId = requestId;
        Timestamp = DateTime.UtcNow;
    }
}

// Packet co payload kieu cu the T.
public class Packet<T> : Packet where T : class
{
    // Payload that su se duoc serialize ra JSON.
    [JsonPropertyName("payload")]
    public T TypedPayload { get; set; } = default!;

    // Tra payload ve dang object theo hop dong cua lop cha.
    [JsonIgnore]
    public override object Payload => TypedPayload;

    // Constructor rong can cho JSON deserialize.
    public Packet() : base() { }

    // Constructor tao packet voi payload cu the.
    public Packet(PacketType type, string source, string target, T payload, string? requestId = null)
        : base(type, source, target, requestId)
    {
        TypedPayload = payload;
    }
}
