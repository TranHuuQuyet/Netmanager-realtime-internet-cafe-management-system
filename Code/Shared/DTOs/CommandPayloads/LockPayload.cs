using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload cho cac command server gui den client.
namespace Shared.DTOs.CommandPayloads;

// Payload yeu cau client khoa may.
public class LockPayload
{
    // Ma may can khoa.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Nguoi/he thong phat lenh.
    [JsonPropertyName("issuedBy")]
    [Required]
    public string IssuedBy { get; set; } = string.Empty;

    // Ly do khoa may.
    [JsonPropertyName("reason")]
    [Required]
    public string Reason { get; set; } = string.Empty;
}
