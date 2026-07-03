using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload cho cac command server gui den client.
namespace Shared.DTOs.CommandPayloads;

// Payload dong bo timer/tinh tien tu server xuong client.
public class TimerPayload
{
    // Ma may nhan timer.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Kieu thue may: Timed/OpenEnded/Extend.
    [JsonPropertyName("rentalMode")]
    [Required]
    public string RentalMode { get; set; } = string.Empty;

    // So giay con lai neu la phien co gio het han.
    [JsonPropertyName("remainingSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, long.MaxValue)]
    public long? RemainingSeconds { get; set; }

    // So giay da su dung.
    [JsonPropertyName("elapsedSeconds")]
    [Range(0, long.MaxValue)]
    public long ElapsedSeconds { get; set; }

    // Thoi diem bat dau phien.
    [JsonPropertyName("startedAt")]
    [Required]
    public DateTimeOffset StartedAt { get; set; }

    // Thoi diem het han neu co.
    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTimeOffset? ExpiresAt { get; set; }

    // Don gia theo gio.
    [JsonPropertyName("ratePerHour")]
    [Range(0, long.MaxValue)]
    public long RatePerHour { get; set; }

    // So phut da tinh tien.
    [JsonPropertyName("chargedMinutes")]
    [Range(0, long.MaxValue)]
    public long ChargedMinutes { get; set; }

    // So tien da tinh bang VND.
    [JsonPropertyName("amountVnd")]
    [Range(0, long.MaxValue)]
    public long AmountVnd { get; set; }

    // So du con lai cua tai khoan khach neu co.
    [JsonPropertyName("remainingBalanceVnd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, long.MaxValue)]
    public long? RemainingBalanceVnd { get; set; }

    // Tong so du/tien da nap cua tai khoan khach neu co.
    [JsonPropertyName("totalBalanceVnd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, long.MaxValue)]
    public long? TotalBalanceVnd { get; set; }

    // So giay su dung con lai tinh theo so du neu co.
    [JsonPropertyName("remainingUsageSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, long.MaxValue)]
    public long? RemainingUsageSeconds { get; set; }

    // true khi sap het gio/tien de client hien canh bao.
    [JsonPropertyName("isWarning")]
    public bool IsWarning { get; set; }

    // true khi server muon client khoa may ngay.
    [JsonPropertyName("shouldLockNow")]
    public bool ShouldLockNow { get; set; }

    // Trang thai billing session.
    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = "Active";
}
