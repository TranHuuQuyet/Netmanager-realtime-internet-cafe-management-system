namespace ServerApp.Database;

public sealed record DatabaseOptions
{
    public const string DefaultRelativePath = "AppData/netmanager.db";

    public string DatabasePath { get; init; } = Path.Combine(AppContext.BaseDirectory, DefaultRelativePath);

    public string AppDataDirectory => Path.GetDirectoryName(DatabasePath) ?? AppContext.BaseDirectory;
}
