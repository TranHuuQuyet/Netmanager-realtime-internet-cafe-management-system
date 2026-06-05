using Microsoft.Data.Sqlite;
using SQLitePCL;
using ServerApp.Auth.Models;
using ServerApp.Auth.Services;
using ServerApp.Database;

Batteries_V2.Init();

var dbPath = PrepareScratchDatabasePath();
DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(dbPath);
AuthRuntime runtime = await AuthBootstrapper.CreateAsync(dbPath);

await AssertCanonicalSeedAsync(database, dbPath);
await AssertAdminLoginAsync(runtime);
await AssertClientLoginAsync(runtime);
await AssertWrongPasswordAsync(runtime);
await AssertWrongMachineAsync(runtime);

Console.WriteLine("PASS G0-05: canonical auth seed/database/admin rule match docs");
Console.WriteLine("PASS G2-01: admin login succeeds with admin / 123 / PC00");
Console.WriteLine("PASS G2-02: client login succeeds with client01 / 123 / PC-01");
Console.WriteLine("PASS G2-03: wrong password is rejected visibly");
Console.WriteLine("PASS G2-04: correct client credentials with wrong machineId are rejected");

static async Task AssertCanonicalSeedAsync(DatabaseRuntime database, string dbPath)
{
    await AssertUserAsync(database.Users, "admin", "PC00", UserRole.Admin);
    await AssertUserAsync(database.Users, "client01", "PC-01", UserRole.Client);
    await AssertUserAsync(database.Users, "client02", "PC-02", UserRole.Client);
    await AssertMachineAsync(database.Machines, "PC00");
    await AssertMachineAsync(database.Machines, "PC-01");
    await AssertMachineAsync(database.Machines, "PC-02");
    await AssertSessionCountAsync(dbPath, 0);
}

static async Task AssertAdminLoginAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("admin", "123", "PC00", UserRole.Admin));

    AssertSuccess(result, AuthStatus.Success, "admin");
    if (result.Session is null)
    {
        throw new InvalidOperationException("G2-01 expected a session for admin login.");
    }

    if (!string.Equals(result.Session.MachineId, "PC00", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("G2-01 session machine mismatch.");
    }
}

static async Task AssertClientLoginAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-01", UserRole.Client));

    AssertSuccess(result, AuthStatus.Success, "client01");
    if (result.Session is null)
    {
        throw new InvalidOperationException("G2-02 expected a session for client login.");
    }

    if (!string.Equals(result.Session.MachineId, "PC-01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("G2-02 session machine mismatch.");
    }
}

static async Task AssertWrongPasswordAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "wrong-password", "PC-01", UserRole.Client));

    AssertFailure(result, AuthStatus.InvalidCredentials, "INVALID_CREDENTIALS", "G2-03");
}

static async Task AssertWrongMachineAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-02", UserRole.Client));

    AssertFailure(result, AuthStatus.AccountMachineMismatch, "ACCOUNT_MACHINE_MISMATCH", "G2-04");
}

static void AssertSuccess(AuthResult result, AuthStatus expectedStatus, string username)
{
    if (!result.IsSuccess)
    {
        throw new InvalidOperationException(
            $"Expected success for {username}, but got {result.Status}: {result.Message}");
    }

    if (result.Status != expectedStatus)
    {
        throw new InvalidOperationException(
            $"Expected status {expectedStatus} for {username}, but got {result.Status}.");
    }

    if (result.ErrorCode is not null)
    {
        throw new InvalidOperationException($"Expected success result for {username} to have no error code.");
    }
}

static void AssertFailure(AuthResult result, AuthStatus expectedStatus, string expectedErrorCode, string testName)
{
    if (result.IsSuccess)
    {
        throw new InvalidOperationException($"{testName} expected failure, but login succeeded.");
    }

    if (result.Status != expectedStatus)
    {
        throw new InvalidOperationException(
            $"{testName} expected {expectedStatus}, but got {result.Status}.");
    }

    if (string.IsNullOrWhiteSpace(result.ErrorCode))
    {
        throw new InvalidOperationException($"{testName} should expose a visible error code.");
    }

    if (!string.Equals(result.ErrorCode, expectedErrorCode, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{testName} expected error code {expectedErrorCode}, but got {result.ErrorCode}.");
    }

    if (string.IsNullOrWhiteSpace(result.Message))
    {
        throw new InvalidOperationException($"{testName} should expose a visible error message.");
    }
}

static async Task AssertUserAsync(
    ServerApp.Database.Contracts.IUserRepository users,
    string username,
    string expectedMachineId,
    UserRole expectedRole)
{
    var user = await users.GetByUsernameAsync(username);
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

static async Task AssertMachineAsync(
    ServerApp.Database.Contracts.IMachineRepository machines,
    string machineId)
{
    var machine = await machines.GetByMachineIdAsync(machineId);
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
