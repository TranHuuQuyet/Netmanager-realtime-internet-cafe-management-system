using Microsoft.Data.Sqlite;
using SQLitePCL;
using ServerApp.Auth.Models;
using ServerApp.Database;

Batteries_V2.Init();

var dbPath = PrepareScratchDatabasePath();

DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(dbPath);

await AssertUserAsync(database, "admin", "PC00", UserRole.Admin);
await AssertUserAsync(database, "client01", "PC-01", UserRole.Client);
await AssertUserAsync(database, "client02", "PC-02", UserRole.Client);
await AssertMachineAsync(database, "PC00");
await AssertMachineAsync(database, "PC-01");
await AssertMachineAsync(database, "PC-02");
await AssertSessionCountAsync(dbPath, 0);

Console.WriteLine("PASS G0-05: canonical auth seed/database/admin rule match docs");

static async Task AssertUserAsync(DatabaseRuntime database, string username, string expectedMachineId, UserRole expectedRole)
{
    var user = await database.Users.GetByUsernameAsync(username);
    if (user is null)
    {
        throw new InvalidOperationException($"Missing canonical user: {username}");
    }

    if (!string.Equals(user.MachineId, expectedMachineId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{username}.MachineId mismatch: expected {expectedMachineId}, got {(user.MachineId ?? "<null>")}");
    }

    if (user.IsActive != true)
    {
        throw new InvalidOperationException($"{username}.IsActive must be true.");
    }

    if (user.Role != expectedRole)
    {
        throw new InvalidOperationException(
            $"{username}.Role mismatch: expected {expectedRole}, got {user.Role}");
    }
}

static async Task AssertMachineAsync(DatabaseRuntime database, string machineId)
{
    var machine = await database.Machines.GetByMachineIdAsync(machineId);
    if (machine is null)
    {
        throw new InvalidOperationException($"Missing canonical machine: {machineId}");
    }

    if (!string.Equals(machine.MachineId, machineId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"MachineId mismatch: expected {machineId}, got {machine.MachineId}");
    }

    if (machine.IsActive != true)
    {
        throw new InvalidOperationException($"{machineId}.IsActive must be true.");
    }
}

static async Task AssertSessionCountAsync(string dbPath, int expectedCount)
{
    await using var connection = new SqliteConnection($"Data Source={dbPath}");
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM AuthSessions;";

    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
    if (count != expectedCount)
    {
        throw new InvalidOperationException($"AuthSessions count mismatch: expected {expectedCount}, got {count}");
    }
}

static string PrepareScratchDatabasePath()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "Netmanager-G0-05");
    Directory.CreateDirectory(tempRoot);

    var dbPath = Path.Combine(tempRoot, "internet_cafe.g0-05.db");
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
    }

    return dbPath;
}