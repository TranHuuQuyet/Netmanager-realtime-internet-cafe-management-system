using ServerApp.Auth.Models;

namespace ServerApp.Database.Entities;

public sealed record AuthSessionEntity(
    string Id,
    string UserId,
    string Username,
    UserRole Role,
    string? MachineId,
    SessionState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);
