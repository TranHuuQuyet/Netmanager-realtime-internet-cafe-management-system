using Microsoft.Data.Sqlite;

namespace ServerApp.Database;

public sealed class SqliteConnectionFactory {
    private readonly string _connectionString;

    public SqliteConnectionFactory(DatabaseOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = $"Data Source={options.DatabasePath}";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
