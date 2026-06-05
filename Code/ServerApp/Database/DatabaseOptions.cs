namespace ServerApp.Database;

public sealed record DatabaseOptions
{
    public const string DefaultRelativePath = DatabasePathResolver.CanonicalDatabaseFileName;

    public string DatabasePath { get; init; } = DefaultRelativePath;

    public string AppDataDirectory => Path.GetDirectoryName(DatabasePath) ?? AppContext.BaseDirectory;
}
