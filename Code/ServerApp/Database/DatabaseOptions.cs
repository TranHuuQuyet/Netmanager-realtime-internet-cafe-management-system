namespace ServerApp.Database;

public sealed record DatabaseOptions {
    public const string DefaultFileName = "internet_cafe.db";

    public DatabaseOptions(string? databasePath = null) {
        DatabasePath = ResolveDatabasePath(databasePath);
    }

    public string DatabasePath { get; init; }

    public string DatabaseDirectory => Path.GetDirectoryName(DatabasePath) ?? AppContext.BaseDirectory;

    public static string ResolveDatabasePath(string? databasePath = null) {
        var path = string.IsNullOrWhiteSpace(databasePath)
            ? DefaultFileName
            : databasePath.Trim();

        if (Path.IsPathRooted(path)) {
            return path;
        }

        return Path.GetFullPath(Path.Combine(ResolveRepositoryRoot(), path));
    }

    private static string ResolveRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null) {
            var candidate = directory.FullName;
            if (Directory.Exists(Path.Combine(candidate, ".git")) ||
                (Directory.Exists(Path.Combine(candidate, "Code")) && Directory.Exists(Path.Combine(candidate, "DOCS")))) {
                return candidate;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
