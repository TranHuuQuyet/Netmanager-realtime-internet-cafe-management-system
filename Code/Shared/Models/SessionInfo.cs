// Namespace chua cac model dung chung.
namespace Shared.Models;

// Thong tin tom tat ve mot session.
public record SessionInfo(Guid Id, Guid UserId, string MachineId, string Status, DateTime StartTime, DateTime? EndTime);
