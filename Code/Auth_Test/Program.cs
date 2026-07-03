using Microsoft.Data.Sqlite;
using SQLitePCL;
using ServerApp.Auth.Models;
using ServerApp.Auth.Services;
using ServerApp.Billing.Models;
using ServerApp.Database;
using ServerApp.Database.Models;
using ServerApp.Presentation;

// Chuong trinh console smoke test cho auth/database/billing.
// Neu mot assert that bai, test se nem exception va dung chuong trinh.
Batteries_V2.Init();

// Dinh dang ma may cu co dau gach ngang, dung de test migration ve PC01/PC02.
const string LegacyMachineSeparator = "-";

// Tao database tam va khoi tao runtime can test.
var dbPath = PrepareScratchDatabasePath();
DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(dbPath);
AuthRuntime runtime = await AuthBootstrapper.CreateAsync(dbPath);

// Chay lan luot cac test chinh cua auth, seed data, command guard va billing recovery.
await AssertCanonicalSeedAsync(database, dbPath);
await AssertAdminLoginAsync(runtime);
await AssertClientLoginAsync(runtime);
await AssertWrongPasswordAsync(runtime);
await AssertWrongMachineAsync(runtime);
await AssertRoleMismatchAsync(runtime);
await AssertUnknownMachineAsync();
await AssertLegacyMachineIdMigrationAsync();
await AssertSeedBootstrapResetsStaleMachineStatusAsync();
await AssertCommandGuardAsync(runtime);
await AssertR4DistinctClientsAsync();
await AssertBillingRecoverySnapshotAsync();
await AssertAutoOpenEndedBillingAsync();

// Neu chay den day nghia la tat ca assert da pass.
Console.WriteLine("PASS G0-05: canonical auth seed/database/admin rule match docs");
Console.WriteLine("PASS G2-01: admin login succeeds with admin / 123 / PC00");
Console.WriteLine("PASS G2-02: client login succeeds with client01 / 123 / PC01");
Console.WriteLine("PASS G2-03: wrong password is rejected visibly");
Console.WriteLine("PASS G2-04: correct client credentials with wrong machineId are rejected");
Console.WriteLine("PASS TC-A08: role mismatch returns INVALID_CREDENTIALS");
Console.WriteLine("PASS TC-A09: unknown machine ID returns INVALID_MACHINE_ID");
Console.WriteLine("PASS G2-05: legacy hyphenated machine IDs migrate to canonical PCXX");
Console.WriteLine("PASS G2-06: server startup resets stale machine status to Offline");
Console.WriteLine("PASS R3-A01: command guard accepts active target and rejects inactive target");
Console.WriteLine("PASS R4-N01: two authenticated clients stay distinct and duplicate active login is rejected");
Console.WriteLine("PASS R4-R01: billing recovery snapshot restores active billing with timer state");
Console.WriteLine("PASS R5-B04: client online auto-starts open-ended billing and repeated sync is idempotent");

static async Task AssertCanonicalSeedAsync(DatabaseRuntime database, string dbPath)
{
    await AssertUserAsync(database.Users, "admin", "PC00", UserRole.Admin);
    await AssertUserAsync(database.Users, "client01", "PC01", UserRole.Client);
    await AssertUserAsync(database.Users, "client02", "PC02", UserRole.Client);
    await AssertMachineAsync(database.Machines, "PC00");
    await AssertMachineAsync(database.Machines, "PC01");
    await AssertMachineAsync(database.Machines, "PC02");
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
        new AuthRequest("client01", "123", "PC01", UserRole.Client));

    AssertSuccess(result, AuthStatus.Success, "client01");
    if (result.Session is null)
    {
        throw new InvalidOperationException("G2-02 expected a session for client login.");
    }

    if (!string.Equals(result.Session.MachineId, "PC01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("G2-02 session machine mismatch.");
    }
}

static async Task AssertWrongPasswordAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "wrong-password", "PC01", UserRole.Client));

    AssertFailure(result, AuthStatus.InvalidCredentials, "INVALID_CREDENTIALS", "G2-03");
}

static async Task AssertWrongMachineAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC02", UserRole.Client));

    AssertFailure(result, AuthStatus.AccountMachineMismatch, "ACCOUNT_MACHINE_MISMATCH", "G2-04");
}

static async Task AssertRoleMismatchAsync(AuthRuntime runtime)
{
    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Admin));

    AssertFailure(result, AuthStatus.RoleMismatch, "INVALID_CREDENTIALS", "TC-A08");
}

static async Task AssertUnknownMachineAsync()
{
    AuthRuntime runtime = await AuthBootstrapper.CreateAsync(PrepareScratchDatabasePath("Netmanager-TC-A09-UnknownMachine"));
    ServerApp.Database.Models.PasswordHash runtimePasswordHash = ServerApp.Database.PasswordHasher.Hash("123");
    await runtime.Users.AddAsync(new UserRecord(
        "user-missing-machine",
        "missingmachine",
        runtimePasswordHash.SaltBase64,
        runtimePasswordHash.HashBase64,
        UserRole.Client,
        "PC99",
        true,
        null));

    var result = await runtime.Auth.AuthenticateAsync(
        new AuthRequest("missingmachine", "123", "PC99", UserRole.Client));

    AssertFailure(result, AuthStatus.InvalidMachineId, "INVALID_MACHINE_ID", "TC-A09");
}

static async Task AssertLegacyMachineIdMigrationAsync()
{
    string legacyDbPath = PrepareScratchDatabasePath("Netmanager-G2-05-LegacyMachineIds");
    await CreateLegacyMachineIdDatabaseAsync(legacyDbPath);

    DatabaseRuntime migratedDatabase = await DatabaseBootstrapper.CreateAsync(legacyDbPath);
    AuthRuntime migratedRuntime = await AuthBootstrapper.CreateAsync(legacyDbPath);

    await AssertUserAsync(migratedDatabase.Users, "client01", "PC01", UserRole.Client);
    await AssertUserAsync(migratedDatabase.Users, "client02", "PC02", UserRole.Client);
    await AssertMachineAsync(migratedDatabase.Machines, "PC01");
    await AssertMachineAsync(migratedDatabase.Machines, "PC02");
    await AssertMachineMissingAsync(migratedDatabase.Machines, LegacyMachineId("01"));
    await AssertMachineMissingAsync(migratedDatabase.Machines, LegacyMachineId("02"));
    await AssertNoLegacyMachineIdsAsync(legacyDbPath);

    var pc01Login = await migratedRuntime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
    AssertSuccess(pc01Login, AuthStatus.Success, "G2-05 client01 migrated login");
    if (pc01Login.Session is null)
    {
        throw new InvalidOperationException("G2-05 expected migrated client01 session.");
    }

    await migratedRuntime.SessionService.CloseSessionAsync(pc01Login.Session.Id);

    var pc02Login = await migratedRuntime.Auth.AuthenticateAsync(
        new AuthRequest("client02", "123", "PC02", UserRole.Client));
    AssertSuccess(pc02Login, AuthStatus.Success, "G2-05 client02 migrated login");
    if (pc02Login.Session is null)
    {
        throw new InvalidOperationException("G2-05 expected migrated client02 session.");
    }

    await migratedRuntime.SessionService.CloseSessionAsync(pc02Login.Session.Id);
}

static async Task AssertSeedBootstrapResetsStaleMachineStatusAsync()
{
    string staleStatusDbPath = PrepareScratchDatabasePath("Netmanager-G2-06-StaleMachineStatus");
    DatabaseRuntime initialDatabase = await DatabaseBootstrapper.CreateAsync(staleStatusDbPath);

    await initialDatabase.Machines.UpdateStatusAsync("PC01", "Online", DateTime.UtcNow);
    await AssertMachineStateAsync(initialDatabase.Machines, "PC01", expectedIsActive: true, expectedStatus: "Online");

    DatabaseRuntime restartedDatabase = await DatabaseBootstrapper.CreateAsync(staleStatusDbPath);
    await AssertMachineStateAsync(restartedDatabase.Machines, "PC01", expectedIsActive: true, expectedStatus: "Offline");
}

static async Task AssertCommandGuardAsync(AuthRuntime runtime)
{
    string guardDbPath = PrepareScratchDatabasePath("Netmanager-R3-A01");
    DatabaseRuntime guardDatabase = await DatabaseBootstrapper.CreateAsync(guardDbPath);
    AuthRuntime guardRuntime = await AuthBootstrapper.CreateAsync(guardDbPath);

    await AssertMachineStateAsync(guardDatabase.Machines, "PC01", expectedIsActive: true, expectedStatus: "Offline");

    var user = await guardDatabase.Users.GetByUsernameAsync("client01");
    if (user is null)
    {
        throw new InvalidOperationException("R3-A01 expected canonical user client01.");
    }

    var session = await guardRuntime.SessionService.OpenSessionAsync(user);

    if (!string.Equals(session.MachineId, "PC01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R3-A01 expected a session bound to PC01.");
    }

    var allowed = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC01");
    AssertCommandGuardSuccess(allowed, "PC01", "R3-A01 active machine");

    var deniedInactive = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC02");
    AssertCommandGuardFailure(deniedInactive, "R3-A01 inactive machine", "UNAUTHORIZED_COMMAND");

    await guardRuntime.SessionService.CloseSessionAsync(session.Id);

    var deniedClosed = await guardRuntime.SessionService.AuthorizeCommandTargetAsync("PC01");
    AssertCommandGuardFailure(deniedClosed, "R3-A01 closed machine", "UNAUTHORIZED_COMMAND");
}

static async Task AssertR4DistinctClientsAsync()
{
    string r4DbPath = PrepareScratchDatabasePath("Netmanager-R4-N01");
    AuthRuntime r4Runtime = await AuthBootstrapper.CreateAsync(r4DbPath);

    var client01 = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
    AssertSuccess(client01, AuthStatus.Success, "R4-N01 client01");

    var client02 = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client02", "123", "PC02", UserRole.Client));
    AssertSuccess(client02, AuthStatus.Success, "R4-N01 client02");

    if (client01.Session is null || client02.Session is null)
    {
        throw new InvalidOperationException("R4-N01 expected active sessions for both clients.");
    }

    if (string.Equals(client01.Session.Id, client02.Session.Id, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R4-N01 expected distinct session IDs for client01 and client02.");
    }

    if (!string.Equals(client01.Session.MachineId, "PC01", StringComparison.Ordinal)
        || !string.Equals(client02.Session.MachineId, "PC02", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R4-N01 expected each client to keep its own machine binding.");
    }

    var duplicateLogin = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
    AssertFailure(
        duplicateLogin,
        AuthStatus.MachineAlreadyActive,
        "MACHINE_ALREADY_ACTIVE",
        "R4-N01 duplicate active login");

    await r4Runtime.SessionService.CloseSessionAsync(client01.Session.Id);
    await r4Runtime.SessionService.CloseSessionAsync(client02.Session.Id);

    var client01Reopened = await r4Runtime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
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
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
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

    var duplicate = await billing.OpenSessionAsync(
        new BillingSessionRequest(
            login.Session.Id,
            login.User.Id,
            login.User.Username,
            login.Session.MachineId,
            BillingRentalMode.OpenEnded,
            DateTimeOffset.UtcNow));

    if (!duplicate.IsFailure || duplicate.ErrorCode != "BILLING_SESSION_ALREADY_ACTIVE")
    {
        throw new InvalidOperationException("R5-B01 expected duplicate active billing to be rejected.");
    }

    var extended = await billing.ExtendSessionAsync(
        opened.Session.Session.Id,
        TimeSpan.FromMinutes(5),
        DateTimeOffset.UtcNow);

    if (!extended.IsSuccess
        || extended.Session is null
        || extended.Session.Session.RentalMode != BillingRentalMode.Extend
        || extended.Session.Session.ExpiresAtUtc <= opened.Session.Session.ExpiresAtUtc)
    {
        throw new InvalidOperationException("R5-B02 expected timed billing extension to update expiry and mode.");
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
    if (!string.Equals(restored.Session.Session.MachineId, "PC01", StringComparison.Ordinal))
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

    AuthRuntime restartedRuntime = await AuthBootstrapper.CreateAsync(billingDbPath);
    var restartedSnapshot = await restartedRuntime.Billing.GetRecoverySnapshotAsync(DateTimeOffset.UtcNow);
    if (restartedSnapshot.Sessions.Count != 1
        || !string.Equals(restartedSnapshot.Sessions[0].Session.Session.MachineId, "PC01", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("R5-R01 expected fresh runtime to restore active billing from SQLite.");
    }

    var calculation = billing.CalculateAmount(
        DateTimeOffset.UtcNow.AddSeconds(-61),
        DateTimeOffset.UtcNow,
        10_000);

    if (calculation.ChargedMinutes != 2)
    {
        throw new InvalidOperationException("R4-R01 expected 61 seconds to round up to 2 charged minutes.");
    }

    var closed = await billing.CloseSessionAsync(opened.Session.Session.Id, DateTimeOffset.UtcNow);
    if (!closed.IsSuccess
        || closed.Session is null
        || closed.Session.Session.State != BillingSessionState.Closed
        || closed.Session.Session.AmountVnd <= 0)
    {
        throw new InvalidOperationException("R5-B01 expected close to persist charged minutes and amount.");
    }

    var openEnded = await billing.OpenSessionAsync(
        new BillingSessionRequest(
            login.Session.Id,
            login.User.Id,
            login.User.Username,
            login.Session.MachineId,
            BillingRentalMode.OpenEnded,
            DateTimeOffset.UtcNow.AddMinutes(-3),
            10_000,
            null));

    if (!openEnded.IsSuccess || openEnded.Session is null)
    {
        throw new InvalidOperationException("R5-B01 expected open-ended billing session to open after close.");
    }

    var openEndedSnapshot = await billing.GetRecoverySnapshotAsync(DateTimeOffset.UtcNow);
    var openEndedSync = openEndedSnapshot.Sessions.SingleOrDefault(session =>
        session.Session.Session.RentalMode == BillingRentalMode.OpenEnded);
    if (openEndedSync is null
        || openEndedSync.RemainingSeconds is not null
        || openEndedSync.ShouldLockNow)
    {
        throw new InvalidOperationException("R5-R01 expected open-ended billing restore without remaining seconds or lock.");
    }

    await billing.CloseSessionAsync(openEnded.Session.Session.Id, DateTimeOffset.UtcNow);
    await authRuntime.SessionService.CloseSessionAsync(login.Session.Id);
}

static async Task AssertAutoOpenEndedBillingAsync()
{
    string billingDbPath = PrepareScratchDatabasePath("Netmanager-R5-B04-AutoBilling");
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(billingDbPath);

    var login = await authRuntime.Auth.AuthenticateAsync(
        new AuthRequest("client01", "123", "PC01", UserRole.Client));
    if (!login.IsSuccess || login.Session is null)
    {
        throw new InvalidOperationException("R5-B04 expected client01 to login before auto billing.");
    }

    var adminBilling = new NetworkAdminBillingService(
        authRuntime.Billing,
        authRuntime.SessionRepository,
        networkServer: null);

    AdminBillingResult opened = await adminBilling.EnsureOpenEndedAsync("PC01");
    if (!opened.IsSuccess
        || opened.Timer is null
        || opened.Timer.RentalMode != BillingRentalMode.OpenEnded.ToString()
        || opened.Timer.RemainingSeconds is not null
        || opened.Timer.AmountVnd <= 0)
    {
        throw new InvalidOperationException("R5-B04 expected online client to auto-start open-ended billing.");
    }

    AdminBillingResult synced = await adminBilling.EnsureOpenEndedAsync("PC01");
    if (!synced.IsSuccess
        || synced.Timer is null
        || synced.Timer.RentalMode != BillingRentalMode.OpenEnded.ToString()
        || synced.Timer.ExpiresAt is not null)
    {
        throw new InvalidOperationException("R5-B04 expected repeated online sync to reuse the active billing session.");
    }

    AdminBillingResult closed = await adminBilling.CloseAsync("PC01");
    if (!closed.IsSuccess
        || closed.Timer is null
        || closed.Timer.Status != BillingSessionState.Closed.ToString())
    {
        throw new InvalidOperationException("R5-B04 expected auto billing to close when client session ends.");
    }
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

static async Task AssertMachineMissingAsync(
    ServerApp.Database.Contracts.IMachineRepository machines,
    string machineId)
{
    var machine = await machines.GetByMachineIdAsync(machineId);
    if (machine is not null)
    {
        throw new InvalidOperationException($"Legacy machine should have been removed: {machineId}");
    }
}

static async Task AssertNoLegacyMachineIdsAsync(string dbPath)
{
    await using var connection = new SqliteConnection($"Data Source={dbPath}");
    await connection.OpenAsync();

    foreach (string tableName in new[] { "AuthUsers", "AuthSessions", "BillingSessions", "Machines" })
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE MachineId LIKE @LegacyPattern;";
        command.Parameters.AddWithValue("@LegacyPattern", $"{LegacyMachineId(string.Empty)}%");
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (count != 0)
        {
            throw new InvalidOperationException($"{tableName} still contains {count} legacy hyphenated machine IDs.");
        }
    }
}

static async Task CreateLegacyMachineIdDatabaseAsync(string dbPath)
{
    string legacyPc01 = LegacyMachineId("01");
    string legacyPc02 = LegacyMachineId("02");

    await using var connection = new SqliteConnection($"Data Source={dbPath}");
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE AuthUsers (
            Id TEXT PRIMARY KEY,
            Username TEXT NOT NULL UNIQUE,
            PasswordSaltBase64 TEXT NOT NULL,
            PasswordHashBase64 TEXT NOT NULL,
            Role INTEGER NOT NULL,
            MachineId TEXT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            LastLoginAtUtc TEXT NULL
        );

        CREATE TABLE Machines (
            Id TEXT PRIMARY KEY,
            MachineId TEXT NOT NULL UNIQUE,
            MachineName TEXT NOT NULL,
            IpAddress TEXT NULL,
            Status TEXT NOT NULL,
            LastSeen TEXT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE AuthSessions (
            Id TEXT PRIMARY KEY,
            UserId TEXT NOT NULL,
            Username TEXT NOT NULL,
            Role INTEGER NOT NULL,
            MachineId TEXT NULL,
            State INTEGER NOT NULL,
            StartedAtUtc TEXT NOT NULL,
            EndedAtUtc TEXT NULL,
            FOREIGN KEY (UserId) REFERENCES AuthUsers(Id)
        );

        CREATE TABLE BillingSessions (
            Id TEXT PRIMARY KEY,
            AuthSessionId TEXT NOT NULL,
            UserId TEXT NOT NULL,
            Username TEXT NOT NULL,
            MachineId TEXT NOT NULL,
            RentalMode INTEGER NOT NULL,
            State INTEGER NOT NULL,
            RatePerHour INTEGER NOT NULL,
            StartedAtUtc TEXT NOT NULL,
            ExpiresAtUtc TEXT NULL,
            EndedAtUtc TEXT NULL,
            ChargedMinutes INTEGER NOT NULL DEFAULT 0,
            AmountVnd INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (AuthSessionId) REFERENCES AuthSessions(Id),
            FOREIGN KEY (UserId) REFERENCES AuthUsers(Id)
        );

        INSERT INTO AuthUsers
            (Id, Username, PasswordSaltBase64, PasswordHashBase64, Role, MachineId, IsActive)
        VALUES
            ('user-client01', 'client01', 'legacy-salt', 'legacy-hash', 1, @LegacyPc01, 1),
            ('user-client02', 'client02', 'legacy-salt', 'legacy-hash', 1, @LegacyPc02, 1);

        INSERT INTO Machines
            (Id, MachineId, MachineName, Status, IsActive)
        VALUES
            ('11111111111111111111111111111111', @LegacyPc01, 'Legacy Computer 01', 'Offline', 1),
            ('22222222222222222222222222222222', @LegacyPc02, 'Legacy Computer 02', 'Offline', 1),
            ('33333333333333333333333333333333', 'PC01', 'Computer 01', 'Offline', 1),
            ('44444444444444444444444444444444', 'PC02', 'Computer 02', 'Offline', 1);

        INSERT INTO AuthSessions
            (Id, UserId, Username, Role, MachineId, State, StartedAtUtc)
        VALUES
            ('legacy-auth-session-01', 'user-client01', 'client01', 1, @LegacyPc01, 1, '2026-06-01T00:00:00.0000000Z');

        INSERT INTO BillingSessions
            (Id, AuthSessionId, UserId, Username, MachineId, RentalMode, State, RatePerHour, StartedAtUtc)
        VALUES
            ('legacy-billing-session-01', 'legacy-auth-session-01', 'user-client01', 'client01', @LegacyPc01, 0, 1, 10000, '2026-06-01T00:00:00.0000000Z');
        """;
    command.Parameters.AddWithValue("@LegacyPc01", legacyPc01);
    command.Parameters.AddWithValue("@LegacyPc02", legacyPc02);

    await command.ExecuteNonQueryAsync();
}

static string LegacyMachineId(string suffix) => $"PC{LegacyMachineSeparator}{suffix}";

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
