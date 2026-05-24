namespace ServerApp.Database;

public sealed class DbInitializer
{
    private readonly DatabaseOptions _options;

    public DbInitializer(DatabaseOptions? options = null)
    {
        _options = options ?? new DatabaseOptions();
    }

    public string DatabasePath => _options.DatabasePath;

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

            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL,
                MachineId TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                LastLogin TEXT NULL,
                CreatedAt TEXT NOT NULL
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

            CREATE TABLE IF NOT EXISTS Sessions (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                MachineId TEXT NULL,
                Status TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NULL,
                LastSeen TEXT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            """;
    }
}
