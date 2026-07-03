using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload chat hai chieu giua server va client.
namespace Shared.DTOs.Bidrectional;

// Du lieu cua goi tin chat.
public class ChatPayload
{
    // Nguoi/may gui tin nhan.
    [JsonPropertyName("sender")]
    [Required]
    public string Sender { get; set; } = string.Empty;

    // Nguoi/may nhan tin nhan.
    [JsonPropertyName("receiver")]
    [Required]
    public string Receiver { get; set; } = string.Empty;

    // Noi dung tin nhan.
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;
}
