using ServerApp.Auth.Models;

namespace ServerApp.Database.Models;

public sealed record PasswordHash(string SaltBase64, string HashBase64);

public sealed record SeedAccount(string Username, string Password, string MachineId, UserRole Role, bool IsActive = true);

public sealed record UserRecord(
    string Id,
    string Username,
    string PasswordSaltBase64,
    string PasswordHashBase64,
    UserRole Role,
    string? MachineId,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);

public sealed record SessionRecord(
    string Id,
    string UserId,
    string Username,
    UserRole Role,
    string? MachineId,
    SessionState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record CustomerRecord(
    string CustomerId,
    string FirstName,
    string LastName,
    string Phone,
    string IdentityNumber,
    string Birthday,
    string Username,
    string Password,
    long AccountBalance);
