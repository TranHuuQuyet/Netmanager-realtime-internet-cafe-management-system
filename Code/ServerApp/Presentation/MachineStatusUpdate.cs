namespace ServerApp;

public sealed record MachineStatusUpdate(
    string MachineId,
    string Status,
    DateTime? LastSeenUtc = null);
