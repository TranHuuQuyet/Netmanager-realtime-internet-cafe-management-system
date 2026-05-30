using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.Bidrectional;

public class ChatPayload
{
    [JsonPropertyName("sender")]
    [Required]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("receiver")]
    [Required]
    public string Receiver { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;
}
