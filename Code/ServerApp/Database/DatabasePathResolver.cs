namespace ServerApp.Database;

internal static class DatabasePathResolver
{
    public const string CanonicalDatabaseFileName = "internet_cafe.db";

    public static string Resolve(string? databasePath = null)
    {
        var path = string.IsNullOrWhiteSpace(databasePath) ? CanonicalDatabaseFileName : databasePath.Trim();
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
}
