using System.Globalization;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using ServerApp.Auth.Models;
using ServerApp.Database.Contracts;
using ServerApp.Database.Entities;
using ServerApp.Database.Models;

namespace ServerApp.Database;

public static class DatabaseBootstrapper
{
    public static async Task<DatabaseRuntime> CreateAsync(string? databasePath = null, CancellationToken cancellationToken = default)
    {
        Batteries_V2.Init();

        var store = new DatabaseStore(databasePath);
        await store.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await MigrateLegacyMachineIdsAsync(store, cancellationToken).ConfigureAwait(false);

        var users = new SqliteUserRepository(store);
        var sessions = new SqliteSessionRepository(store);
        var machines = new SqliteMachineRepository(store);
        var billingSessions = new SqliteBillingSessionRepository(store);
        var customers = new SqliteCustomerRepository(store);

        await SeedUsersAsync(users, cancellationToken).ConfigureAwait(false);
        await SeedMachinesAsync(machines, cancellationToken).ConfigureAwait(false);

        return new DatabaseRuntime(users, sessions, machines, billingSessions, customers);
    }

    private static async Task MigrateLegacyMachineIdsAsync(DatabaseStore store, CancellationToken cancellationToken)
    {
        foreach ((string Legacy, string Canonical) mapping in LegacyMachineIdMappings)
        {
            await using var connection = store.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();

            await ExecuteMachineIdMigrationAsync(
                connection,
                transaction,
                "UPDATE AuthUsers SET MachineId = @Canonical WHERE MachineId = @Legacy;",
                mapping.Legacy,
                mapping.Canonical,
                cancellationToken).ConfigureAwait(false);

            await ExecuteMachineIdMigrationAsync(
                connection,
                transaction,
                "UPDATE AuthSessions SET MachineId = @Canonical WHERE MachineId = @Legacy;",
                mapping.Legacy,
                mapping.Canonical,
                cancellationToken).ConfigureAwait(false);

            await ExecuteMachineIdMigrationAsync(
                connection,
                transaction,
                "UPDATE BillingSessions SET MachineId = @Canonical WHERE MachineId = @Legacy;",
                mapping.Legacy,
                mapping.Canonical,
                cancellationToken).ConfigureAwait(false);

            await ExecuteMachineIdMigrationAsync(
                connection,
                transaction,
                """
                UPDATE Machines
                SET MachineId = @Canonical
                WHERE MachineId = @Legacy
                  AND NOT EXISTS (SELECT 1 FROM Machines WHERE MachineId = @Canonical);
                """,
                mapping.Legacy,
                mapping.Canonical,
                cancellationToken).ConfigureAwait(false);

            await ExecuteMachineIdMigrationAsync(
                connection,
                transaction,
                """
                DELETE FROM Machines
                WHERE MachineId = @Legacy
                  AND EXISTS (SELECT 1 FROM Machines WHERE MachineId = @Canonical);
                """,
                mapping.Legacy,
                mapping.Canonical,
                cancellationToken).ConfigureAwait(false);

            transaction.Commit();
        }
    }

    private static async Task ExecuteMachineIdMigrationAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        string legacyMachineId,
        string canonicalMachineId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.Parameters.AddWithValue("@Legacy", legacyMachineId);
        command.Parameters.AddWithValue("@Canonical", canonicalMachineId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly IReadOnlyList<(string Legacy, string Canonical)> LegacyMachineIdMappings =
        new List<(string Legacy, string Canonical)>
        {
            new(CreateLegacyMachineId("01"), "PC01"),
            new(CreateLegacyMachineId("02"), "PC02")
        };

    private const string LegacyMachineSeparator = "-";

    private static string CreateLegacyMachineId(string suffix) => $"PC{LegacyMachineSeparator}{suffix}";

    private static IReadOnlyList<SeedAccount> BuildSeedUsers()
        => new List<SeedAccount>
        {
            new("admin", "123", "PC00", UserRole.Admin, true),
            new("client01", "123", "PC01", UserRole.Client, true),
            new("client02", "123", "PC02", UserRole.Client, true)
        };

    private static async Task SeedUsersAsync(SqliteUserRepository users, CancellationToken cancellationToken)
    {
        foreach (var seed in BuildSeedUsers())
        {
            var existing = await users.GetByUsernameAsync(seed.Username, cancellationToken).ConfigureAwait(false);
            var record = CreateUserRecord(seed);

            if (existing is null)
            {
                await users.AddAsync(record, cancellationToken).ConfigureAwait(false);
                continue;
            }

            await users.UpdateSeedAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SeedMachinesAsync(SqliteMachineRepository machines, CancellationToken cancellationToken)
    {
        foreach (var machine in BuildSeedMachines())
        {
            await machines.UpsertAsync(machine, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<MachineEntity> BuildSeedMachines()
        => SeedData.Machines;

    private static UserRecord CreateUserRecord(SeedAccount seed)
    {
        var hash = PasswordHasher.Hash(seed.Password);

        return new UserRecord(
            $"user-{seed.Username}",
            seed.Username,
            hash.SaltBase64,
            hash.HashBase64,
            seed.Role,
            string.IsNullOrWhiteSpace(seed.MachineId) ? null : seed.MachineId.Trim(),
            seed.IsActive,
            null);
    }

    private static Guid StableGuid(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}

internal sealed class DatabaseStore
{
    private readonly string _connectionString;

    public DatabaseStore(string? databasePath = null)
    {
        _connectionString = $"Data Source={ResolveDatabasePath(databasePath)}";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = GetSchemaScript();
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveDatabasePath(string? databasePath)
    {
        var options = new DatabaseOptions();
        var path = string.IsNullOrWhiteSpace(databasePath) ? options.DatabasePath : databasePath.Trim();
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(ResolveRepositoryRoot(), path));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = directory.FullName;
            if (Directory.Exists(Path.Combine(candidate, ".git")) ||
                (Directory.Exists(Path.Combine(candidate, "Code")) && Directory.Exists(Path.Combine(candidate, "DOCS"))))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static string GetSchemaScript()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "DatabaseSchema.sql");
        if (File.Exists(schemaPath))
        {
            return File.ReadAllText(schemaPath);
        }

        return """
               PRAGMA foreign_keys = ON;

               CREATE TABLE IF NOT EXISTS AuthUsers (
                   Id TEXT PRIMARY KEY,
                   Username TEXT NOT NULL UNIQUE,
                   PasswordSaltBase64 TEXT NOT NULL,
                   PasswordHashBase64 TEXT NOT NULL,
                   Role INTEGER NOT NULL,
                   MachineId TEXT NULL,
                   IsActive INTEGER NOT NULL DEFAULT 1,
                   LastLoginAtUtc TEXT NULL
               );

               CREATE TABLE IF NOT EXISTS Machines (
                   Id TEXT PRIMARY KEY,
                   MachineId TEXT NOT NULL UNIQUE,
                   MachineName TEXT NOT NULL,
                   IpAddress TEXT NULL,
                   Status TEXT NOT NULL,
                   LastSeen TEXT NULL,
                   IsActive INTEGER NOT NULL DEFAULT 1
               );

               CREATE TABLE IF NOT EXISTS AuthSessions (
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

               CREATE TABLE IF NOT EXISTS BillingSessions (
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

               CREATE TABLE IF NOT EXISTS Customers (
                   CustomerId TEXT PRIMARY KEY,
                   FirstName TEXT NOT NULL,
                   LastName TEXT NOT NULL,
                   Phone TEXT NOT NULL,
                   IdentityNumber TEXT NOT NULL,
                   Birthday TEXT NOT NULL,
                   Username TEXT NOT NULL UNIQUE,
                   Password TEXT NOT NULL,
                   AccountBalance INTEGER NOT NULL DEFAULT 0
               );

               CREATE INDEX IF NOT EXISTS IX_BillingSessions_MachineId_State ON BillingSessions (MachineId, State);
               CREATE INDEX IF NOT EXISTS IX_BillingSessions_AuthSessionId_State ON BillingSessions (AuthSessionId, State);
               CREATE INDEX IF NOT EXISTS IX_Customers_Username ON Customers (Username);
               """;
    }
}

internal sealed class SqliteCustomerRepository : ICustomerRepository
{
    private readonly DatabaseStore _store;

    public SqliteCustomerRepository(DatabaseStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<CustomerRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var customers = new List<CustomerRecord>();

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CustomerId, FirstName, LastName, Phone, IdentityNumber, Birthday, Username, Password, AccountBalance
            FROM Customers
            ORDER BY CustomerId;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            customers.Add(ReadCustomer(reader));
        }

        return customers;
    }

    public async Task<CustomerRecord?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CustomerId, FirstName, LastName, Phone, IdentityNumber, Birthday, Username, Password, AccountBalance
            FROM Customers
            WHERE CustomerId = @CustomerId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@CustomerId", customerId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadCustomer(reader);
    }

    public async Task<CustomerRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CustomerId, FirstName, LastName, Phone, IdentityNumber, Birthday, Username, Password, AccountBalance
            FROM Customers
            WHERE Username = @Username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadCustomer(reader);
    }

    public async Task AddAsync(CustomerRecord customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Customers
                (CustomerId, FirstName, LastName, Phone, IdentityNumber, Birthday, Username, Password, AccountBalance)
            VALUES
                (@CustomerId, @FirstName, @LastName, @Phone, @IdentityNumber, @Birthday, @Username, @Password, @AccountBalance);
            """;
        BindCustomer(command, customer);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerRecord customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Customers
            SET FirstName = @FirstName,
                LastName = @LastName,
                Phone = @Phone,
                IdentityNumber = @IdentityNumber,
                Birthday = @Birthday,
                Username = @Username,
                Password = @Password,
                AccountBalance = @AccountBalance
            WHERE CustomerId = @CustomerId;
            """;
        BindCustomer(command, customer);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Customers WHERE CustomerId = @CustomerId;";
        command.Parameters.AddWithValue("@CustomerId", customerId.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void BindCustomer(SqliteCommand command, CustomerRecord customer)
    {
        command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
        command.Parameters.AddWithValue("@FirstName", customer.FirstName);
        command.Parameters.AddWithValue("@LastName", customer.LastName);
        command.Parameters.AddWithValue("@Phone", customer.Phone);
        command.Parameters.AddWithValue("@IdentityNumber", customer.IdentityNumber);
        command.Parameters.AddWithValue("@Birthday", customer.Birthday);
        command.Parameters.AddWithValue("@Username", customer.Username);
        command.Parameters.AddWithValue("@Password", customer.Password);
        command.Parameters.AddWithValue("@AccountBalance", customer.AccountBalance);
    }

    private static CustomerRecord ReadCustomer(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetInt64(8));
}

internal sealed class SqliteUserRepository : IUserRepository
{
    private readonly DatabaseStore _store;

    public SqliteUserRepository(DatabaseStore store)
    {
        _store = store;
    }

    public async Task<UserRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username, PasswordSaltBase64, PasswordHashBase64, Role, MachineId, IsActive, LastLoginAtUtc
            FROM AuthUsers
            WHERE Username = @Username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadUserAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserRecord?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username, PasswordSaltBase64, PasswordHashBase64, Role, MachineId, IsActive, LastLoginAtUtc
            FROM AuthUsers
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", userId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadUserAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM AuthUsers;";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task AddAsync(UserRecord user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO AuthUsers
            (
                Id,
                Username,
                PasswordSaltBase64,
                PasswordHashBase64,
                Role,
                MachineId,
                IsActive,
                LastLoginAtUtc
            )
            VALUES
            (
                @Id,
                @Username,
                @PasswordSaltBase64,
                @PasswordHashBase64,
                @Role,
                @MachineId,
                @IsActive,
                @LastLoginAtUtc
            );
            """;
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@PasswordSaltBase64", user.PasswordSaltBase64);
        command.Parameters.AddWithValue("@PasswordHashBase64", user.PasswordHashBase64);
        command.Parameters.AddWithValue("@Role", (int)user.Role);
        command.Parameters.AddWithValue("@MachineId", (object?)user.MachineId ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@LastLoginAtUtc", (object?)user.LastLoginAtUtc?.UtcDateTime.ToString("O") ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateSeedAsync(UserRecord user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AuthUsers
            SET
                PasswordSaltBase64 = @PasswordSaltBase64,
                PasswordHashBase64 = @PasswordHashBase64,
                Role = @Role,
                MachineId = @MachineId,
                IsActive = @IsActive
            WHERE Username = @Username;
            """;
        command.Parameters.AddWithValue("@PasswordSaltBase64", user.PasswordSaltBase64);
        command.Parameters.AddWithValue("@PasswordHashBase64", user.PasswordHashBase64);
        command.Parameters.AddWithValue("@Role", (int)user.Role);
        command.Parameters.AddWithValue("@MachineId", (object?)user.MachineId ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@Username", user.Username);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateLastLoginAtAsync(string userId, DateTimeOffset lastLoginAtUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AuthUsers
            SET LastLoginAtUtc = @LastLoginAtUtc
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", userId.Trim());
        command.Parameters.AddWithValue("@LastLoginAtUtc", lastLoginAtUtc.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<UserRecord?> ReadUserAsync(SqliteDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new UserRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            (UserRole)reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.GetInt32(6) == 1,
            reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

internal sealed class SqliteSessionRepository : ISessionRepository
{
    private readonly DatabaseStore _store;

    public SqliteSessionRepository(DatabaseStore store)
    {
        _store = store;
    }

    public async Task AddAsync(SessionRecord session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AuthSessions
            (
                Id,
                UserId,
                Username,
                Role,
                MachineId,
                State,
                StartedAtUtc,
                EndedAtUtc
            )
            VALUES
            (
                @Id,
                @UserId,
                @Username,
                @Role,
                @MachineId,
                @State,
                @StartedAtUtc,
                @EndedAtUtc
            );
            """;
        command.Parameters.AddWithValue("@Id", session.Id);
        command.Parameters.AddWithValue("@UserId", session.UserId);
        command.Parameters.AddWithValue("@Username", session.Username);
        command.Parameters.AddWithValue("@Role", (int)session.Role);
        command.Parameters.AddWithValue("@MachineId", (object?)session.MachineId ?? DBNull.Value);
        command.Parameters.AddWithValue("@State", (int)session.State);
        command.Parameters.AddWithValue("@StartedAtUtc", session.StartedAtUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("@EndedAtUtc", (object?)session.EndedAtUtc?.UtcDateTime.ToString("O") ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionRecord?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, Username, Role, MachineId, State, StartedAtUtc, EndedAtUtc
            FROM AuthSessions
            WHERE UserId = @UserId AND State = @State
            ORDER BY StartedAtUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        command.Parameters.AddWithValue("@State", (int)SessionState.Active);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSessionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionRecord?> GetActiveByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, Username, Role, MachineId, State, StartedAtUtc, EndedAtUtc
            FROM AuthSessions
            WHERE MachineId = @MachineId AND State = @State
            ORDER BY StartedAtUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@MachineId", machineId.Trim());
        command.Parameters.AddWithValue("@State", (int)SessionState.Active);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSessionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionRecord?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, Username, Role, MachineId, State, StartedAtUtc, EndedAtUtc
            FROM AuthSessions
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", sessionId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSessionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AuthSessions
            SET State = @RevokedState,
                EndedAtUtc = @EndedAtUtc
            WHERE UserId = @UserId AND State = @ActiveState;
            """;
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        command.Parameters.AddWithValue("@RevokedState", (int)SessionState.Revoked);
        command.Parameters.AddWithValue("@ActiveState", (int)SessionState.Active);
        command.Parameters.AddWithValue("@EndedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStateAsync(string sessionId, SessionState state, DateTimeOffset endedAtUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AuthSessions
            SET State = @State,
                EndedAtUtc = @EndedAtUtc
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", sessionId.Trim());
        command.Parameters.AddWithValue("@State", (int)state);
        command.Parameters.AddWithValue("@EndedAtUtc", endedAtUtc.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SessionRecord?> ReadSessionAsync(SqliteDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new SessionRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            (UserRole)reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            (SessionState)reader.GetInt32(5),
            DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

internal sealed class SqliteBillingSessionRepository : IBillingSessionRepository
{
    private readonly DatabaseStore _store;

    public SqliteBillingSessionRepository(DatabaseStore store)
    {
        _store = store;
    }

    public async Task AddAsync(BillingSessionRecord session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BillingSessions
                (Id, AuthSessionId, UserId, Username, MachineId, RentalMode, State, RatePerHour, StartedAtUtc, ExpiresAtUtc, EndedAtUtc, ChargedMinutes, AmountVnd)
            VALUES
                (@Id, @AuthSessionId, @UserId, @Username, @MachineId, @RentalMode, @State, @RatePerHour, @StartedAtUtc, @ExpiresAtUtc, @EndedAtUtc, @ChargedMinutes, @AmountVnd);
            """;
        BindSession(command, session);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSessionRecord?> GetByIdAsync(string billingSessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(billingSessionId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, AuthSessionId, UserId, Username, MachineId, RentalMode, State, RatePerHour, StartedAtUtc, ExpiresAtUtc, EndedAtUtc, ChargedMinutes, AmountVnd
            FROM BillingSessions
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", billingSessionId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSessionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSessionRecord?> GetActiveByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, AuthSessionId, UserId, Username, MachineId, RentalMode, State, RatePerHour, StartedAtUtc, ExpiresAtUtc, EndedAtUtc, ChargedMinutes, AmountVnd
            FROM BillingSessions
            WHERE MachineId = @MachineId AND State = @State
            ORDER BY StartedAtUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@MachineId", machineId.Trim());
        command.Parameters.AddWithValue("@State", (int)BillingSessionState.Active);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadSessionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BillingSessionRecord>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<BillingSessionRecord>();

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, AuthSessionId, UserId, Username, MachineId, RentalMode, State, RatePerHour, StartedAtUtc, ExpiresAtUtc, EndedAtUtc, ChargedMinutes, AmountVnd
            FROM BillingSessions
            WHERE State = @State
            ORDER BY StartedAtUtc DESC;
            """;
        command.Parameters.AddWithValue("@State", (int)BillingSessionState.Active);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            sessions.Add(ReadSession(reader));
        }

        return sessions;
    }

    public async Task UpdateAsync(BillingSessionRecord session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE BillingSessions
            SET AuthSessionId = @AuthSessionId,
                UserId = @UserId,
                Username = @Username,
                MachineId = @MachineId,
                RentalMode = @RentalMode,
                State = @State,
                RatePerHour = @RatePerHour,
                StartedAtUtc = @StartedAtUtc,
                ExpiresAtUtc = @ExpiresAtUtc,
                EndedAtUtc = @EndedAtUtc,
                ChargedMinutes = @ChargedMinutes,
                AmountVnd = @AmountVnd
            WHERE Id = @Id;
            """;
        BindSession(command, session);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CloseAsync(string billingSessionId, DateTimeOffset endedAtUtc, long chargedMinutes, long amountVnd, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(billingSessionId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE BillingSessions
            SET State = @State,
                EndedAtUtc = @EndedAtUtc,
                ChargedMinutes = @ChargedMinutes,
                AmountVnd = @AmountVnd
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", billingSessionId.Trim());
        command.Parameters.AddWithValue("@State", (int)BillingSessionState.Closed);
        command.Parameters.AddWithValue("@EndedAtUtc", endedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@ChargedMinutes", chargedMinutes);
        command.Parameters.AddWithValue("@AmountVnd", amountVnd);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void BindSession(SqliteCommand command, BillingSessionRecord session)
    {
        command.Parameters.AddWithValue("@Id", session.Id);
        command.Parameters.AddWithValue("@AuthSessionId", session.AuthSessionId);
        command.Parameters.AddWithValue("@UserId", session.UserId);
        command.Parameters.AddWithValue("@Username", session.Username);
        command.Parameters.AddWithValue("@MachineId", session.MachineId);
        command.Parameters.AddWithValue("@RentalMode", (int)session.RentalMode);
        command.Parameters.AddWithValue("@State", (int)session.State);
        command.Parameters.AddWithValue("@RatePerHour", session.RatePerHour);
        command.Parameters.AddWithValue("@StartedAtUtc", session.StartedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@ExpiresAtUtc", session.ExpiresAtUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? (object?)DBNull.Value);
        command.Parameters.AddWithValue("@EndedAtUtc", session.EndedAtUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? (object?)DBNull.Value);
        command.Parameters.AddWithValue("@ChargedMinutes", session.ChargedMinutes);
        command.Parameters.AddWithValue("@AmountVnd", session.AmountVnd);
    }

    private static async Task<BillingSessionRecord?> ReadSessionAsync(SqliteDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadSession(reader);
    }

    private static BillingSessionRecord ReadSession(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            (BillingRentalMode)reader.GetInt32(5),
            (BillingSessionState)reader.GetInt32(6),
            reader.GetInt64(7),
            DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(10) ? null : DateTimeOffset.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetInt64(11),
            reader.GetInt64(12));
}

internal sealed class SqliteMachineRepository : IMachineRepository
{
    private readonly DatabaseStore _store;

    public SqliteMachineRepository(DatabaseStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<MachineEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, MachineId, MachineName, IpAddress, Status, LastSeen, IsActive
            FROM Machines
            ORDER BY MachineId;
            """;

        var machines = new List<MachineEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            machines.Add(ReadMachine(reader));
        }

        return machines;
    }

    public async Task<MachineEntity?> GetByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return null;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, MachineId, MachineName, IpAddress, Status, LastSeen, IsActive
            FROM Machines
            WHERE MachineId = @MachineId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@MachineId", machineId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadMachine(reader);
    }

    public async Task UpdateStatusAsync(string machineId, string status, DateTime lastSeenUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(machineId))
        {
            return;
        }

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Machines
            SET Status = @Status,
                LastSeen = @LastSeen
            WHERE MachineId = @MachineId;
            """;
        command.Parameters.AddWithValue("@MachineId", machineId.Trim());
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@LastSeen", lastSeenUtc.ToUniversalTime().ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(MachineEntity machine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machine);

        await using var connection = _store.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Machines
            (
                Id,
                MachineId,
                MachineName,
                IpAddress,
                Status,
                LastSeen,
                IsActive
            )
            VALUES
            (
                @Id,
                @MachineId,
                @MachineName,
                @IpAddress,
                @Status,
                @LastSeen,
                @IsActive
            )
            ON CONFLICT(MachineId) DO UPDATE SET
                MachineName = excluded.MachineName,
                IpAddress = excluded.IpAddress,
                IsActive = excluded.IsActive;
            """;
        command.Parameters.AddWithValue("@Id", machine.Id.ToString("N"));
        command.Parameters.AddWithValue("@MachineId", machine.MachineId);
        command.Parameters.AddWithValue("@MachineName", machine.MachineName);
        command.Parameters.AddWithValue("@IpAddress", (object?)machine.IpAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", machine.Status);
        command.Parameters.AddWithValue("@LastSeen", (object?)machine.LastSeen?.ToUniversalTime().ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", machine.IsActive ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MachineEntity ReadMachine(SqliteDataReader reader)
    {
        return new MachineEntity
        {
            Id = Guid.Parse(reader.GetString(0)),
            MachineId = reader.GetString(1),
            MachineName = reader.GetString(2),
            IpAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
            Status = reader.GetString(4),
            LastSeen = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            IsActive = reader.GetInt32(6) == 1
        };
    }
}
