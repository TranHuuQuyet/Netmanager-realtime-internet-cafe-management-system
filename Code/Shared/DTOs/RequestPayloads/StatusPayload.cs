using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace chua payload request client gui len server.
namespace Shared.DTOs.RequestPayloads;

// Payload client gui de bao trang thai may.
public class StatusPayload
{
    // Ma may dang bao trang thai.
    [JsonPropertyName("machineId")]
    [Required]
    public string MachineId { get; set; } = string.Empty;

    // Session hien tai neu may dang co phien dang nhap.
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    // Ten hien thi cua may.
    [JsonPropertyName("machineName")]
    [Required]
    public string MachineName { get; set; } = string.Empty;

    // Trang thai may, vi du Online/Offline/InUse.
    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; } = string.Empty;

    // Dia chi IP cua may.
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    // Thoi diem cap nhat trang thai gan nhat.
    [JsonPropertyName("lastSeen")]
    [Required]
    public DateTime LastSeen { get; set; }
}
