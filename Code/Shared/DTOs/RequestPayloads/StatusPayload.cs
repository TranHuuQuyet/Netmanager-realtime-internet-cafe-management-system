using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.RequestPayloads;

public class StatusPayload
{
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("machineName")]
    [Required]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("lastSeen")]
    [Required]
    public DateTime LastSeen { get; set; }
}
