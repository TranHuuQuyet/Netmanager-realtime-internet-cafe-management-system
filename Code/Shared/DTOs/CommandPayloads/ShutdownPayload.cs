using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload cho cac command server gui den client.
namespace Shared.DTOs.CommandPayloads;

// Payload yeu cau client tat may/ung dung.
public class ShutdownPayload
{
    // Ma may can shutdown.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Nguoi/he thong phat lenh.
    [JsonPropertyName("issuedBy")]
    [Required]
    public string IssuedBy { get; set; } = string.Empty;

    // Ly do shutdown.
    [JsonPropertyName("reason")]
    [Required]
    public string Reason { get; set; } = string.Empty;
}
