using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NETManager.Shared.DTOs.CommandPayloads;

public class NotificationPayload
{
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    [Required]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    [Required]
    public string Scope { get; set; } = string.Empty;
}
