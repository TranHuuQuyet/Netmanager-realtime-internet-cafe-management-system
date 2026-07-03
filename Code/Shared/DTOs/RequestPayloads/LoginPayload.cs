using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload request client gui len server.
namespace Shared.DTOs.RequestPayloads;

// Payload dang nhap tu client/server UI.
public class LoginPayload
{
    // Ten dang nhap.
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; } = string.Empty;

    // Mat khau dang nhap.
    [JsonPropertyName("password")]
    [Required]
    public string Password { get; set; } = string.Empty;

    // Vai tro dang nhap, vi du Admin/Client.
    [JsonPropertyName("role")]
    [Required]
    public string Role { get; set; } = string.Empty;

    // Ma may dang dang nhap.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;
}
