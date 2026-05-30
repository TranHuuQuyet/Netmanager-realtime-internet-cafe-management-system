using System.Text.Json.Serialization;

namespace Shared.Models;

public class ErrorInfo
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
