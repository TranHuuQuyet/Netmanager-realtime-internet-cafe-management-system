namespace ServerApp.Database.Entities;
public sealed record SessionEntity
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string MachineId { get; init; } =
        string.Empty;

    public string Status { get; init; } =
        string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime? EndedAt { get; init; }

    public DateTime? LastSeen { get; init; }
}