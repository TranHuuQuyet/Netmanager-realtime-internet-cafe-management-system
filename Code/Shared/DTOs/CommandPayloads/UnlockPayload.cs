using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload cho cac command server gui den client.
namespace Shared.DTOs.CommandPayloads;

// Payload yeu cau client mo khoa may.
public class UnlockPayload
{
    // Ma may can mo khoa.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Nguoi/he thong phat lenh.
    [JsonPropertyName("issuedBy")]
    [Required]
    public string IssuedBy { get; set; } = string.Empty;

    // Ly do mo khoa.
    [JsonPropertyName("reason")]
    [Required]
    public string Reason { get; set; } = string.Empty;
}
