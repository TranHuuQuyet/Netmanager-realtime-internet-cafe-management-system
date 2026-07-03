// Namespace cua tang Database.
namespace ServerApp.Database;

// Record chua cac tuy chon cau hinh lien quan den database.
public sealed record DatabaseOptions
{
    // Duong dan mac dinh cua file SQLite tinh tu thu muc ung dung/repo.
    public const string DefaultRelativePath = "AppData/netmanager.db";

    // Duong dan file database se dung.
    // Mac dinh dat trong AppData/netmanager.db ben duoi thu muc chay app.
    public string DatabasePath { get; init; } = Path.Combine(AppContext.BaseDirectory, DefaultRelativePath);

    // Thu muc chua file database.
    // Neu khong lay duoc thu muc tu DatabasePath thi quay ve thu muc chay app.
    public string AppDataDirectory => Path.GetDirectoryName(DatabasePath) ?? AppContext.BaseDirectory;
}
