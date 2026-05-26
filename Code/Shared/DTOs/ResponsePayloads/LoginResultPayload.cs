using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NETManager.Shared.DTOs.ResponsePayloads;

/// <summary>
/// Successful LOGIN response payload.
/// </summary>
public record LoginResultPayload
{
    /// <summary>Server-issued session identifier.</summary>
    [JsonPropertyName("sessionId")]
    [Required]
    public string SessionId { get; init; } = string.Empty;

    /// <summary>The authenticated username.</summary>
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>The authenticated user's currently-assigned role.</summary>
    [JsonPropertyName("role")]
    [Required]
    public string Role { get; init; } = string.Empty;

    /// <summary>The authenticated user's machine identifier.</summary>
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; init; } = string.Empty;
}
