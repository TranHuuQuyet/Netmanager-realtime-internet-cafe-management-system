// Namespace cua tang Database.
namespace ServerApp.Database;

// Lop ho tro khoi tao database: tao thu muc va lay script tao bang.
public sealed class DbInitializer
{
    // Cau hinh duong dan database.
    private readonly DatabaseOptions _options;

    // Constructor nhan options tuy chon.
    // Neu khong truyen vao thi dung DatabaseOptions mac dinh.
    public DbInitializer(DatabaseOptions? options = null)
    {
        _options = options ?? new DatabaseOptions();
    }

    // Cho phep code ben ngoai doc duong dan file database dang dung.
    public string DatabasePath => _options.DatabasePath;

    // Dam bao thu muc AppData ton tai truoc khi tao/mo file database.
    public void EnsureAppDataDirectory()
    {
        Directory.CreateDirectory(_options.AppDataDirectory);
    }

    // Lay script SQL de tao schema database.
    // Uu tien file DatabaseSchema.sql neu ton tai, neu khong thi dung fallback nhung san trong code.
    public string GetSchemaScript()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "DatabaseSchema.sql");
        return File.Exists(schemaPath) ? File.ReadAllText(schemaPath) : EmbeddedSchemaFallback.Script;
    }

    // Lop long ben trong chua SQL fallback.
    // private nghia la chi DbInitializer moi dung duoc lop nay.
    private static class EmbeddedSchemaFallback
    {
        // Raw string literal nhieu dong, bat dau va ket thuc bang ba dau ".
        // Script nay tao cac bang co ban neu chung chua ton tai.
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
            """;
    }
}
