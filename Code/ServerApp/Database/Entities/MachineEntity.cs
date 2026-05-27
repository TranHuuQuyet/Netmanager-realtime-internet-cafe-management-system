namespace ServerApp.Database.Entities;

public sealed record MachineEntity
{
    public Guid Id { get; init; }
    public string MachineId { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? LastSeen { get; init; }
    public bool IsActive { get; init; }
}
