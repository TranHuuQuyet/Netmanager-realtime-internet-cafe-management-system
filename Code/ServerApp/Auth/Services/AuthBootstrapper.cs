using ServerApp.Auth.Contracts;
using ServerApp.Billing.Contracts;
using ServerApp.Billing.Services;
using ServerApp.Database;
using ServerApp.Database.Contracts;

namespace ServerApp.Auth.Services;

// Gom runtime auth day du: repository, session service, billing, va auth service.
public sealed record AuthRuntime(
    IUserRepository Users,
    ISessionRepository SessionRepository,
    IMachineRepository Machines,
    ICustomerRepository Customers,
    ISessionService SessionService,
    IAuthService Auth,
    IBillingService Billing);

public static class AuthBootstrapper
{
    private const string CanonicalDatabasePath = "internet_cafe.db";

    // Composition root cho auth runtime; persistence bootstrap nam trong DatabaseBootstrapper.
    public static async Task<AuthRuntime> CreateAsync(
        string? databasePath = CanonicalDatabasePath,
        CancellationToken cancellationToken = default)
    {
        DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);

        ISessionService sessionService = new SessionService(database.Sessions, database.Machines);
        IAuthService auth = new AuthService(database.Users, database.Machines, sessionService, database.Customers);
        IBillingService billing = new BillingService(database.BillingSessions);

        return new AuthRuntime(
            database.Users,
            database.Sessions,
            database.Machines,
            database.Customers,
            sessionService,
            auth,
            billing);
    }

    // Helper cho UI form khi chi can IAuthService, khong can dereference AuthRuntime.
    public static async Task<IAuthService> CreateAuthServiceAsync(
        string? databasePath = CanonicalDatabasePath,
        CancellationToken cancellationToken = default)
    {
        AuthRuntime runtime = await CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);
        return runtime.Auth;
    }
}
