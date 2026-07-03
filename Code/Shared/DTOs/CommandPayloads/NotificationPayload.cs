using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload cho cac command server gui den client.
namespace Shared.DTOs.CommandPayloads;

// Payload hien thong bao tren may client.
public class NotificationPayload
{
    // Noi dung thong bao.
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    // Muc do thong bao, vi du Info/Warning/Error.
    [JsonPropertyName("severity")]
    [Required]
    public string Severity { get; set; } = string.Empty;

    // Pham vi thong bao, vi du Direct/Broadcast.
    [JsonPropertyName("scope")]
    [Required]
    public string Scope { get; set; } = string.Empty;
}
