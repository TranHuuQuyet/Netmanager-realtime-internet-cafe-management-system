using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.CommandPayloads;

public class TimerPayload
{
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("rentalMode")]
    [Required]
    public string RentalMode { get; set; } = string.Empty;

    [JsonPropertyName("remainingSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, long.MaxValue)]
    public long? RemainingSeconds { get; set; }

    [JsonPropertyName("startedAt")]
    [Required]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("ratePerHour")]
    [Range(0, long.MaxValue)]
    public long RatePerHour { get; set; }

    [JsonPropertyName("chargedMinutes")]
    [Range(0, long.MaxValue)]
    public long ChargedMinutes { get; set; }

    [JsonPropertyName("amountVnd")]
    [Range(0, long.MaxValue)]
    public long AmountVnd { get; set; }

    [JsonPropertyName("isWarning")]
    public bool IsWarning { get; set; }

    [JsonPropertyName("shouldLockNow")]
    public bool ShouldLockNow { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = "Active";
}
