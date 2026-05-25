using System.Globalization;
using Microsoft.Data.Sqlite;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp.Auth.Services;

// Gom runtime auth day du: repository, session service, va auth service.
public sealed record AuthRuntime(
    IUserRepository Users,
    ISessionRepository SessionRepository,
    ISessionService SessionService,
    IAuthService Auth);

public static class AuthBootstrapper {
    // Tao database, schema, seed data va dependency graph cho auth.
    public static async Task<AuthRuntime> CreateAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        var database = new AuthDatabase(databasePath);
        await database.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var users = new SqliteUserRepository(database);
        ISessionRepository sessionRepository = new SqliteSessionRepository(database);
        ISessionService sessionService = new SessionService(sessionRepository);
        IAuthService auth = new AuthService(users, sessionService);

        await SeedUsersAsync(users, cancellationToken).ConfigureAwait(false);

        return new AuthRuntime(users, sessionRepository, sessionService, auth);
    }

    // Chi seed user neu username chua ton tai, de khong ghi de du lieu co san.
    private static async Task SeedUsersAsync(SqliteUserRepository users, CancellationToken cancellationToken) {
        foreach (var seed in BuildSeedUsers()) {
            if (await users.UsernameExistsAsync(seed.Username, cancellationToken).ConfigureAwait(false)) {
                continue;
            }

            await users.AddAsync(CreateUserRecord(seed), cancellationToken).ConfigureAwait(false);
        }
    }

    // Danh sach tai khoan mac dinh cho demo/local test.
    private static IReadOnlyList<SeedAccount> BuildSeedUsers() {
        return new List<SeedAccount> {
            new("admin", "123", string.Empty, UserRole.Admin, true),
            new("client01", "123", "PC-01", UserRole.Client, true),
            new("client02", "123", "PC-02", UserRole.Client, true)
        };
    }

    // Chuyen seed account thanh ban ghi user day du, co hash mat khau va machine mapping.
    private static UserRecord CreateUserRecord(SeedAccount seed) {
        var hash = PasswordHasher.Hash(seed.Password);
        var machineId = string.IsNullOrWhiteSpace(seed.MachineId) ? null : seed.MachineId.Trim();

        return new UserRecord(
            $"user-{seed.Username}",
            seed.Username,
            hash.SaltBase64,
            hash.HashBase64,
            seed.Role,
            machineId,
            seed.IsActive,
            null);
    }
}

// Lop phu tro tao connection string va khoi tao schema SQLite cho auth.
internal sealed class AuthDatabase {
    private readonly string _connectionString;

    public AuthDatabase(string? databasePath = null) {
        var path = string.IsNullOrWhiteSpace(databasePath) ? "internet_cafe.db" : databasePath.Trim();
        _connectionString = $"Data Source={path}";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    // Tao 2 bang AuthUsers va AuthSessions neu chua ton tai.
    public async Task InitializeAsync(CancellationToken cancellationToken = default) {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            CREATE TABLE IF NOT EXISTS AuthUsers
            (
                Id TEXT PRIMARY KEY,
                Username TEXT NOT NULL UNIQUE,
                PasswordSaltBase64 TEXT NOT NULL,
                PasswordHashBase64 TEXT NOT NULL,
                Role INTEGER NOT NULL,
                MachineId TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                LastLoginAtUtc TEXT
            );

            CREATE TABLE IF NOT EXISTS AuthSessions
            (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Username TEXT NOT NULL,
                Role INTEGER NOT NULL,
                MachineId TEXT,
                State INTEGER NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                EndedAtUtc TEXT
            );
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

// Repository SQLite cho user: phuc vu login, seed va cap nhat lan dang nhap cuoi.
internal sealed class SqliteUserRepository : IUserRepository {
    private readonly AuthDatabase _database;

    public SqliteUserRepository(AuthDatabase database) {
        _database = database;
    }

    // Tim user theo username de auth service so khop password va role.
    public async Task<UserRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(username)) {
            return null;
        }

        await using var connection = _database.CreateConnection();
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

    // Tim user theo Id khi can tham chieu lai record da co.
    public async Task<UserRecord?> GetByIdAsync(string userId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return null;
        }

        await using var connection = _database.CreateConnection();
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

    // Dem so ban ghi user trong DB, dung cho smoke check hoac seed logic.
    public async Task<int> CountAsync(CancellationToken cancellationToken = default) {
        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM AuthUsers;";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    // Chen moi hoac ghi de record user theo Id.
    public async Task AddAsync(UserRecord user, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = _database.CreateConnection();
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

    // Cap nhat thoi diem dang nhap cuoi cua user.
    public async Task UpdateLastLoginAtAsync(string userId, DateTimeOffset lastLoginAtUtc, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return;
        }

        await using var connection = _database.CreateConnection();
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

    // Kiem tra username da ton tai de tranh seed trung lap.
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(username)) {
            return false;
        }

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM AuthUsers
            WHERE Username = @Username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username.Trim());

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    // Doc dong SQLite va map ve UserRecord.
    private static async Task<UserRecord?> ReadUserAsync(SqliteDataReader reader, CancellationToken cancellationToken) {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
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

// Repository SQLite cho session: tao session moi, lay session active, va dong/revoke session.
internal sealed class SqliteSessionRepository : ISessionRepository {
    private readonly AuthDatabase _database;

    public SqliteSessionRepository(AuthDatabase database) {
        _database = database;
    }

    // Luu session moi khi login thanh cong.
    public async Task AddAsync(SessionRecord session, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _database.CreateConnection();
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

    // Lay session active moi nhat cua mot user.
    public async Task<SessionRecord?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return null;
        }

        await using var connection = _database.CreateConnection();
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

    // Lay session theo Id de phuc vu dong session hoac debug.
    public async Task<SessionRecord?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sessionId)) {
            return null;
        }

        await using var connection = _database.CreateConnection();
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

    // Huy tat ca session active cu cua user truoc khi mo session moi.
    public async Task RevokeActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return;
        }

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AuthSessions
            SET State = @RevokedState,
                EndedAtUtc = @EndedAtUtc
            WHERE UserId = @UserId AND State = @ActiveState;
            """;
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        command.Parameters.AddWithValue("@ActiveState", (int)SessionState.Active);
        command.Parameters.AddWithValue("@RevokedState", (int)SessionState.Revoked);
        command.Parameters.AddWithValue("@EndedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    // Cap nhat trang thai session va thoi diem ket thuc.
    public async Task UpdateStateAsync(string sessionId, SessionState state, DateTimeOffset endedAtUtc, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sessionId)) {
            return;
        }

        await using var connection = _database.CreateConnection();
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

    // Parse ket qua query thanh SessionRecord domain object.
    private static async Task<SessionRecord?> ReadSessionAsync(SqliteDataReader reader, CancellationToken cancellationToken) {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
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
