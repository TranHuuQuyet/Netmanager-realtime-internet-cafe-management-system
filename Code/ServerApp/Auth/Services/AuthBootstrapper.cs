using ServerApp.Auth.Contracts;
using ServerApp.Database;
using ServerApp.Database.Contracts;

namespace ServerApp.Auth.Services;

// Gom runtime auth day du: repository, session service, va auth service.
public sealed record AuthRuntime(
    IUserRepository Users,
    ISessionRepository SessionRepository,
    ISessionService SessionService,
    IAuthService Auth);

public static class AuthBootstrapper {
    // Composition root cho auth runtime; persistence bootstrap nam trong DatabaseBootstrapper.
    public static async Task<AuthRuntime> CreateAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        DatabaseRuntime database = await DatabaseBootstrapper.CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);

        ISessionService sessionService = new SessionService(database.Sessions);
        IAuthService auth = new AuthService(database.Users, sessionService);

        return new AuthRuntime(database.Users, database.Sessions, sessionService, auth);
    }

    // Helper cho UI form khi chi can IAuthService, khong can dereference AuthRuntime.
    public static async Task<IAuthService> CreateAuthServiceAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        var runtime = await CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);
        return runtime.Auth;
    }
}
