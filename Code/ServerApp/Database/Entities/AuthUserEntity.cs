using ServerApp.Auth.Models;

namespace ServerApp.Database.Entities;

public sealed record AuthUserEntity(
    string Id,
    string Username,
    string PasswordSaltBase64,
    string PasswordHashBase64,
    UserRole Role,
    string? MachineId,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);
