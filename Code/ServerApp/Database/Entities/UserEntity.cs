namespace ServerApp.Database.Entities;

public sealed record UserEntity
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? MachineId { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLogin { get; init; }
    public DateTime CreatedAt { get; init; }
}
