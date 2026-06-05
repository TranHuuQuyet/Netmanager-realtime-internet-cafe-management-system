using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.RequestPayloads;

public class AckPayload
{
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("ackFor")]
    [Required]
    public string AckFor { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
