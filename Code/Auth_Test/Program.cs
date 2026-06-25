using Microsoft.Data.Sqlite;
using SQLitePCL;
using ServerApp.Auth.Models;
using ServerApp.Auth.Services;
using ServerApp.Billing.Models;
using ServerApp.Database;
using ServerApp.Database.Models;

Batteries_V2.Init();

var dbPath = PrepareScratchDatabasePath();
DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(dbPath);
AuthRuntime runtime = await AuthBootstrapper.CreateAsync(dbPath);

await AssertCanonicalSeedAsync(database, dbPath);
await AssertAdminLoginAsync(runtime);
await AssertClientLoginAsync(runtime);
await AssertWrongPasswordAsync(runtime);
await AssertWrongMachineAsync(runtime);
await AssertCommandGuardAsync(runtime);
await AssertR4DistinctClientsAsync();
await AssertBillingRecoverySnapshotAsync();

Console.WriteLine("PASS G0-05: canonical auth seed/database/admin rule match docs");
Console.WriteLine("PASS G2-01: admin login succeeds with admin / 123 / PC00");
Console.WriteLine("PASS G2-02: client login succeeds with client01 / 123 / PC-01");
Console.WriteLine("PASS G2-03: wrong password is rejected visibly");
Console.WriteLine("PASS G2-04: correct client credentials with wrong machineId are rejected");
Console.WriteLine("PASS R3-A01: command guard accepts active target and rejects inactive target");
Console.WriteLine("PASS R4-N01: two authenticated clients stay distinct and duplicate active login is rejected");
Console.WriteLine("PASS R4-R01: billing recovery snapshot restores active billing with timer state");

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

static async Task AssertCommandGuardAsync(AuthRuntime runtime)
{
    string guardDbPath = PrepareScratchDatabasePath("Netmanager-R3-A01");
    DatabaseRuntime guardDatabase = await DatabaseBootstrapper.CreateAsync(guardDbPath);
    AuthRuntime guardRuntime = await AuthBootstrapper.CreateAsync(guardDbPath);

    await AssertMachineStateAsync(guardDatabase.Machines, "PC-01", expectedIsActive: true, expectedStatus: "Offline");

    var user = await guardDatabase.Users.GetByUsernameAsync("client01");
    if (user is null)
    {
        throw new InvalidOperationException("R3-A01 expected canonical user client01.");
    }

    var session = await guardRuntime.SessionService.OpenSessionAsync(user);

    if (!string.Equals(session.MachineId, "PC-01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R3-A01 expected a session bound to PC-01.");
    }

    var allowed = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC-01");
    AssertCommandGuardSuccess(allowed, "PC-01", "R3-A01 active machine");

    var deniedInactive = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC-02");
    AssertCommandGuardFailure(deniedInactive, "R3-A01 inactive machine", "UNAUTHORIZED_COMMAND");

    await guardRuntime.SessionService.CloseSessionAsync(session.Id);

    var deniedClosed = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC-01");
    AssertCommandGuardFailure(deniedClosed, "R3-A01 closed machine", "UNAUTHORIZED_COMMAND");
}

static async Task AssertR4DistinctClientsAsync()
{
    string r4DbPath = PrepareScratchDatabasePath("Netmanager-R4-N01");
    AuthRuntime r4Runtime = await AuthBootstrapper.CreateAsync(r4DbPath);

    var client01 = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-01", UserRole.Client));
    AssertSuccess(client01, AuthStatus.Success, "R4-N01 client01");

    var client02 = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client02", "123", "PC-02", UserRole.Client));
    AssertSuccess(client02, AuthStatus.Success, "R4-N01 client02");

    if (client01.Session is null || client02.Session is null)
    {
        throw new InvalidOperationException("R4-N01 expected active sessions for both clients.");
    }

    if (string.Equals(client01.Session.Id, client02.Session.Id, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R4-N01 expected distinct session IDs for client01 and client02.");
    }

    if (!string.Equals(client01.Session.MachineId, "PC-01", StringComparison.Ordinal)
        || !string.Equals(client02.Session.MachineId, "PC-02", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R4-N01 expected each client to keep its own machine binding.");
    }

    var duplicateLogin = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-01", UserRole.Client));
    AssertFailure(
        duplicateLogin,
        AuthStatus.MachineAlreadyActive,
        "MACHINE_ALREADY_ACTIVE",
        "R4-N01 duplicate active login");

    await r4Runtime.SessionService.CloseSessionAsync(client01.Session.Id);
    await r4Runtime.SessionService.CloseSessionAsync(client02.Session.Id);

    var client01Reopened = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-01", UserRole.Client));
    AssertSuccess(client01Reopened, AuthStatus.Success, "R4-N01 client01 reopened");

    if (client01Reopened.Session is null)
    {
        throw new InvalidOperationException("R4-N01 expected a session after reopening client01.");
    }

    await r4Runtime.SessionService.CloseSessionAsync(client01Reopened.Session.Id);
}

static async Task AssertBillingRecoverySnapshotAsync()
{
    string billingDbPath = PrepareScratchDatabasePath("Netmanager-R4-R01-Billing");
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(billingDbPath);
    var billing = authRuntime.Billing;

    var login = await authRuntime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC-01", UserRole.Client));
    if (!login.IsSuccess || login.User is null || login.Session is null)
    {
        throw new InvalidOperationException("R4-R01 expected a valid authenticated client session.");
    }

    var opened = await billing.OpenSessionAsync(
        new BillingSessionRequest(
            login.Session.Id,
            login.User.Id,
            login.User.Username,
            login.Session.MachineId,
            BillingRentalMode.Timed,
            DateTimeOffset.UtcNow.AddMinutes(-17),
            10_000,
            DateTimeOffset.UtcNow.AddMinutes(13)));

    if (!opened.IsSuccess || opened.Session is null)
    {
        throw new InvalidOperationException("R4-R01 expected billing session open to succeed.");
    }

    var active = await billing.GetActiveSessionAsync(login.Session.MachineId, DateTimeOffset.UtcNow);
    if (active is null || active.Session is null)
    {
        throw new InvalidOperationException("R4-R01 expected active billing lookup to succeed.");
    }

    if (active.Session.Calculation.ChargedMinutes < 17)
    {
        throw new InvalidOperationException("R4-R01 expected active billing lookup to restore charged minutes.");
    }

    var snapshot = await billing.GetRecoverySnapshotAsync(DateTimeOffset.UtcNow);
    if (snapshot.Sessions.Count != 1)
    {
        throw new InvalidOperationException(
            $"R4-R01 expected one active billing session, got {snapshot.Sessions.Count}.");
    }

    var restored = snapshot.Sessions[0];
    if (!string.Equals(restored.Session.Session.MachineId, "PC-01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R4-R01 expected billing snapshot to keep machine binding.");
    }

    if (restored.ShouldLockNow)
    {
        throw new InvalidOperationException("R4-R01 expected the session to still have remaining time.");
    }

    if (restored.RemainingSeconds is null || restored.RemainingSeconds <= 0)
    {
        throw new InvalidOperationException("R4-R01 expected positive remaining seconds for active billing.");
    }

    if (restored.Session.Calculation.ChargedMinutes < 17)
    {
        throw new InvalidOperationException("R4-R01 expected rounded-up charged minutes to be restored.");
    }

    var calculation = billing.CalculateAmount(
        DateTimeOffset.UtcNow.AddSeconds(-61),
        DateTimeOffset.UtcNow,
        10_000);

    if (calculation.ChargedMinutes != 2)
    {
        throw new InvalidOperationException("R4-R01 expected 61 seconds to round up to 2 charged minutes.");
    }

    await billing.CloseSessionAsync(opened.Session.Session.Id, DateTimeOffset.UtcNow);
    await authRuntime.SessionService.CloseSessionAsync(login.Session.Id);
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

static void AssertCommandGuardSuccess(AuthResult result, string expectedMachineId, string testName)
{
    AssertSuccess(result, AuthStatus.Success, testName);

    if (result.Session is null)
    {
        throw new InvalidOperationException($"{testName} expected an active session result.");
    }

    if (!string.Equals(result.Session.MachineId, expectedMachineId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{testName} expected machine {expectedMachineId}, but got {result.Session.MachineId}.");
    }
}

static void AssertCommandGuardFailure(AuthResult result, string testName, string expectedErrorCode)
{
    AssertFailure(result, AuthStatus.UnauthorizedCommand, expectedErrorCode, testName);
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

static async Task AssertMachineStateAsync(
    ServerApp.Database.Contracts.IMachineRepository machines,
    string machineId,
    bool expectedIsActive,
    string expectedStatus)
{
    var machine = await machines.GetByMachineIdAsync(machineId);
    if (machine is null)
    {
        throw new InvalidOperationException($"Missing machine state check target: {machineId}");
    }

    if (machine.IsActive != expectedIsActive)
    {
        throw new InvalidOperationException(
            $"{machineId}.IsActive mismatch: expected {expectedIsActive}, got {machine.IsActive}");
    }

    if (!string.Equals(machine.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"{machineId}.Status mismatch: expected {expectedStatus}, got {machine.Status}");
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

static string PrepareScratchDatabasePath(string? scenarioName = null)
{
    var tempRoot = Path.Combine(Path.GetTempPath(), scenarioName ?? "Netmanager-G0-05");
    Directory.CreateDirectory(tempRoot);

    var dbPath = Path.Combine(tempRoot, "internet_cafe.g0-05.db");
    DeleteIfExists(dbPath);
    DeleteIfExists($"{dbPath}-journal");
    DeleteIfExists($"{dbPath}-wal");
    DeleteIfExists($"{dbPath}-shm");

    return dbPath;
}

static void DeleteIfExists(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}
