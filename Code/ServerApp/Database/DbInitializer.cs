using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;
using ServerApp.Auth.Services;

namespace ServerApp.Database;

public sealed class DbInitializer {
    private readonly DatabaseOptions _options;
    private readonly SqliteConnectionFactory _connections;

    public DbInitializer(DatabaseOptions options, SqliteConnectionFactory connections) {
        _options = options;
        _connections = connections;
    }

    public string DatabasePath => _options.DatabasePath;

    public async Task InitializeAsync(CancellationToken cancellationToken = default) {
        Directory.CreateDirectory(_options.DatabaseDirectory);

        await using var connection = _connections.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = GetSchemaScript();
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SeedAsync(IUserRepository users, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(users);

        foreach (var seed in BuildSeedUsers()) {
            if (await users.GetByUsernameAsync(seed.Username, cancellationToken).ConfigureAwait(false) is not null) {
                continue;
            }

            await users.AddAsync(CreateUserRecord(seed), cancellationToken).ConfigureAwait(false);
        }
    }

    public static string GetSchemaScript() {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "Schema", "AuthSchema.sql");
        return File.ReadAllText(schemaPath);
    }

    private static IReadOnlyList<SeedAccount> BuildSeedUsers() {
        return new List<SeedAccount> {
            new("admin", "123", "PC00", UserRole.Admin, true),
            new("client01", "123", "PC-01", UserRole.Client, true),
            new("client02", "123", "PC-02", UserRole.Client, true)
        };
    }

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
