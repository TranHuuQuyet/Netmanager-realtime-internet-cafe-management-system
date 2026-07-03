using System.Text.Json.Serialization;

// Namespace chua cac model dung chung.
namespace Shared.Models;

// Thong tin loi di kem trong packet.
public class ErrorInfo
{
    // Ma loi ngan gon de code xu ly.
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    // Mo ta chi tiet tuy chon.
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
