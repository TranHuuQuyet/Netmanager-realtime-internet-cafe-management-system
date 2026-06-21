using ServerApp.Billing.Contracts;
using ServerApp.Database;
using ServerApp.Database.Contracts;

namespace ServerApp.Billing.Services;

public sealed record BillingRuntime(
    IBillingSessionRepository BillingSessions,
    IBillingService Billing,
    DatabaseRuntime Database);

public static class BillingBootstrapper
{
    private const string CanonicalDatabasePath = "internet_cafe.db";

    public static async Task<BillingRuntime> CreateAsync(
        string? databasePath = CanonicalDatabasePath,
        CancellationToken cancellationToken = default)
    {
        DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);
        IBillingService billing = new BillingService(database.BillingSessions);
        return new BillingRuntime(database.BillingSessions, billing, database);
    }
}
