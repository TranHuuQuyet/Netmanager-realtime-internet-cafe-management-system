using Shared.Enums;

// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Yeu cau admin gui lenh dieu khien mot may, vi du LOCK/UNLOCK/SHUTDOWN.
public sealed record AdminCommandRequest(
    string MachineId,
    CommandType Command,
    string IssuedBy,
    string Reason);
