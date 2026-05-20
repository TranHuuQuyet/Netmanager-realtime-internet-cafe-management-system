namespace NETManager.Shared.Models;

public record SessionInfo(Guid Id, Guid UserId, string MachineId, string Status, DateTime StartTime, DateTime? EndTime);
