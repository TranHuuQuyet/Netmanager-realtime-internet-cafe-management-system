namespace ServerApp.Database;

public sealed class DbInitializer
{
    private readonly DatabaseOptions _options;

    public DbInitializer(DatabaseOptions? options = null)
    {
        _options = options ?? new DatabaseOptions();
    }

    public string DatabasePath => DatabasePathResolver.Resolve(_options.DatabasePath);

    public void EnsureAppDataDirectory()
    {
        Directory.CreateDirectory(_options.AppDataDirectory);
    }

    public string GetSchemaScript()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "DatabaseSchema.sql");
        return File.Exists(schemaPath) ? File.ReadAllText(schemaPath) : EmbeddedSchemaFallback.Script;
    }

    private static class EmbeddedSchemaFallback
    {
        public const string Script = """
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

            CREATE INDEX IF NOT EXISTS IX_AuthUsers_Username ON AuthUsers (Username);
            CREATE INDEX IF NOT EXISTS IX_AuthUsers_MachineId ON AuthUsers (MachineId);
            CREATE INDEX IF NOT EXISTS IX_AuthSessions_UserId_State ON AuthSessions (UserId, State);
            CREATE INDEX IF NOT EXISTS IX_AuthSessions_MachineId_State ON AuthSessions (MachineId, State);
            """;
    }
}
