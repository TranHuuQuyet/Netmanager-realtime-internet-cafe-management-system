using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.CommandPayloads;

public class UnlockPayload
{
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("issuedBy")]
    [Required]
    public string IssuedBy { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    [Required]
    public string Reason { get; set; } = string.Empty;
}
