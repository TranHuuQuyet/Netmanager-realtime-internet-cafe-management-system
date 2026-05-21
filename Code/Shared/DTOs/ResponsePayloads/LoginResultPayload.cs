using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NETManager.Shared.DTOs.ResponsePayloads;

/// <summary>
/// Successful LOGIN response payload.
/// Immutable record — set once at construction, never modified.
/// </summary>
public record LoginResultPayload
{
    /// <summary>Server-issued session token (opaque string).</summary>
    [JsonPropertyName("token")]
    [Required]
    public string Token { get; init; } = string.Empty;

    /// <summary>Server-issued session identifier.</summary>
    [JsonPropertyName("sessionId")]
    [Required]
    public Guid SessionId { get; init; }

    /// <summary>The authenticated user's machine identifier.</summary>
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; init; } = string.Empty;

    /// <summary>The authenticated user's currently-assigned role.</summary>
    [JsonPropertyName("role")]
    [Required]
    public string Role { get; init; } = string.Empty;

    /// <summary>Client IP address as seen by the server.</summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }

    /// <summary>Server UTC timestamp when the session was created.</summary>
    [JsonPropertyName("expiresAt")]
    [Required]
    public DateTime ExpiresAt { get; init; }

    /// <summary>Server UTC timestamp of this response.</summary>
    [JsonPropertyName("issuedAt")]
    [Required]
    public DateTime IssuedAt { get; init; }
}

/// <summary>
/// Failed LOGIN response payload.
/// Immutable record — set once at construction, never modified.
/// </summary>
public record LoginFailedPayload
{
    /// <summary>Machine-readable error code.</summary>
    [JsonPropertyName("errorCode")]
    [Required]
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Human-readable failure description shown to the user.</summary>
    [JsonPropertyName("errorMessage")]
    [Required]
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Whether the client should allow another login attempt.
    /// false = the client should lock or wait before retrying (e.g. account disabled).
    /// </summary>
    [JsonPropertyName("retryable")]
    public bool Retryable { get; init; } = true;

    /// <summary>Server UTC timestamp of this response.</summary>
    [JsonPropertyName("issuedAt")]
    [Required]
    public DateTime IssuedAt { get; init; }
}
