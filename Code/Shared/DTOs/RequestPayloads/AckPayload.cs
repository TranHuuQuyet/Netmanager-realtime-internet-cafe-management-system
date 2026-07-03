using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload request client gui len server.
namespace Shared.DTOs.RequestPayloads;

// Payload ACK de client bao server da nhan/xu ly mot command.
public class AckPayload
{
    // Ma may gui ACK.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Ten/type command ma ACK nay phan hoi.
    [JsonPropertyName("ackFor")]
    [Required]
    public string AckFor { get; set; } = string.Empty;

    // Trang thai xu ly command, vi du Success/Failed/Ignored.
    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = string.Empty;

    // Thong bao chi tiet tuy chon.
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
