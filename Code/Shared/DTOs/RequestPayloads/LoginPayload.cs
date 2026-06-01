using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.RequestPayloads;

public class LoginPayload
{
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    [Required]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    [Required]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;
}
