using System;
using System.Collections.Generic;
using ServerApp.Database.Entities;

// Namespace cua tang Database.
namespace ServerApp.Database;

// Lop tinh chua du lieu mau ban dau cho he thong.
// static nghia la khong can tao object SeedData moi dung duoc cac thanh vien ben trong.
public static class SeedData
{
    // Thoi diem tao co dinh cho du lieu mau.
    // Dung gia tri co dinh giup moi lan chay tao ra du lieu nhat quan.
    private static readonly DateTime CreatedAt = new(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc);

    // Danh sach user mau.
    // IReadOnlyList cho phep doc danh sach nhung khong sua truc tiep tu ben ngoai.
    public static IReadOnlyList<UserEntity> Users { get; } = new List<UserEntity>
    {
        CreateUser("admin", "Admin", "PC00"),
        CreateUser("client01", "Client", "PC01"),
        CreateUser("client02", "Client", "PC02"),
        CreateUser("client03", "Client", "PC03"),
        CreateUser("client04", "Client", "PC04"),
        CreateUser("client05", "Client", "PC05"),
        CreateUser("client06", "Client", "PC06"),
        CreateUser("client07", "Client", "PC07"),
        CreateUser("client08", "Client", "PC08")
    };

    // Danh sach may mau tuong ung voi cac may trong quan net.
    public static IReadOnlyList<MachineEntity> Machines { get; } = new List<MachineEntity>
    {
        CreateMachine("PC00", "Computer 00"),
        CreateMachine("PC01", "Computer 01"),
        CreateMachine("PC02", "Computer 02"),
        CreateMachine("PC03", "Computer 03"),
        CreateMachine("PC04", "Computer 04"),
        CreateMachine("PC05", "Computer 05"),
        CreateMachine("PC06", "Computer 06"),
        CreateMachine("PC07", "Computer 07"),
        CreateMachine("PC08", "Computer 08")
    };

    // Ham tao user mau de tranh lap code trong danh sach Users.
    private static UserEntity CreateUser(string username, string role, string? machineId)
    {
        // Object initializer gan gia tri cho cac property cua UserEntity.
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

    // Ham tao may mau de tranh lap code trong danh sach Machines.
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

    // Tao Guid on dinh tu chuoi dau vao.
    // Khac Guid.NewGuid(), cung mot value se luon ra cung mot Guid.
    private static Guid StableGuid(string value)
    {
        // Bam SHA256 chuoi dau vao, lay 16 byte dau de tao Guid.
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}
