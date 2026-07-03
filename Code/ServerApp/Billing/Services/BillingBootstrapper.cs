using ServerApp.Billing.Contracts;
using ServerApp.Database;
using ServerApp.Database.Contracts;

// Namespace chua cac service billing.
namespace ServerApp.Billing.Services;

// Goi cac thanh phan billing da khoi tao xong de app dung.
public sealed record BillingRuntime(
    // Repository luu/doc phien tinh tien.
    IBillingSessionRepository BillingSessions,

    // Service chua logic tinh tien.
    IBillingService Billing,

    // Runtime database day du de cac tang khac dung chung.
    DatabaseRuntime Database);

// Lop khoi tao billing runtime.
public static class BillingBootstrapper
{
    // File SQLite mac dinh cua ung dung.
    private const string CanonicalDatabasePath = "internet_cafe.db";

    // Tao database runtime, tao billing service, roi goi chung lai thanh BillingRuntime.
    public static async Task<BillingRuntime> CreateAsync(
        string? databasePath = CanonicalDatabasePath,
        CancellationToken cancellationToken = default)
    {
        DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);
        IBillingService billing = new BillingService(database.BillingSessions);
        return new BillingRuntime(database.BillingSessions, billing, database);
    }
}
