using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NETManager.Shared.DTOs.CommandPayloads;

public class TimerPayload
{
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("remainingSeconds")]
    [Range(0, int.MaxValue)]
    public int RemainingSeconds { get; set; }

    [JsonPropertyName("startedAt")]
    [Required]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    [Required]
    public DateTime ExpiresAt { get; set; }
}
