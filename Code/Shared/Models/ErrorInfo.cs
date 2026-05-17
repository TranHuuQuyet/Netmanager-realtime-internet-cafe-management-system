using System.Text.Json.Serialization;

namespace NETManager.Shared.Models;

public class ErrorInfo
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
