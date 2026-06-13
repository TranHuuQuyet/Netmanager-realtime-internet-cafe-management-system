using Shared.Enums;

namespace ServerApp.Presentation;

public sealed record AdminCommandRequest(
    string MachineId,
    CommandType Command,
    string IssuedBy,
    string Reason);
