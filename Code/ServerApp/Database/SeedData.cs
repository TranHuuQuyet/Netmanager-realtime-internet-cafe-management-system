using System;
using System.Collections.Generic;
using ServerApp.Database.Entities;

namespace ServerApp.Database;

public static class SeedData
{
    private static readonly DateTime CreatedAt = new(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<UserEntity> Users { get; } = new List<UserEntity>
    {
        CreateUser("admin", "Admin", "PC00"),
        CreateUser("client01", "Client", "PC01"),
        CreateUser("client02", "Client", "PC02")
    };

    public static IReadOnlyList<MachineEntity> Machines { get; } = new List<MachineEntity>
    {
        CreateMachine("PC00", "Computer 00"),
        CreateMachine("PC01", "Computer 01"),
        CreateMachine("PC02", "Computer 02")
    };

    private static UserEntity CreateUser(string username, string role, string? machineId)
    {
        return new UserEntity
        {
            Id = StableGuid($"user:{username}"),
            Username = username,
            Password = "123",
            Role = role,
            MachineId = machineId,
            IsActive = true,
            CreatedAt = CreatedAt
        };
    }

    private static MachineEntity CreateMachine(string machineId, string machineName)
    {
        return new MachineEntity
        {
            Id = StableGuid($"machine:{machineId}"),
            MachineId = machineId,
            MachineName = machineName,
            Status = "Offline",
            IsActive = true
        };
    }

    private static Guid StableGuid(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}
