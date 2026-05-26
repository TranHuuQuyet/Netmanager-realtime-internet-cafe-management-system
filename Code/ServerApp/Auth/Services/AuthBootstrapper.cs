using ServerApp.Auth.Contracts;
using ServerApp.Database;
using ServerApp.Database.Repositories;

namespace ServerApp.Auth.Services;

// Gom runtime auth day du: repository, session service, va auth service.
public sealed record AuthRuntime(
    IUserRepository Users,
    ISessionRepository SessionRepository,
    ISessionService SessionService,
    IAuthService Auth);

public static class AuthBootstrapper {
    // Composition root cho auth/database runtime.
    public static async Task<AuthRuntime> CreateAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        var options = new DatabaseOptions(databasePath);
        var connections = new SqliteConnectionFactory(options);
        var initializer = new DbInitializer(options, connections);

        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);

        IUserRepository users = new SqliteUserRepository(connections);
        ISessionRepository sessionRepository = new SqliteSessionRepository(connections);
        ISessionService sessionService = new SessionService(sessionRepository);
        IAuthService auth = new AuthService(users, sessionService);

        await initializer.SeedAsync(users, cancellationToken).ConfigureAwait(false);

        return new AuthRuntime(users, sessionRepository, sessionService, auth);
    }

    // Helper cho UI form khi chi can IAuthService, khong can dereference AuthRuntime.
    public static async Task<IAuthService> CreateAuthServiceAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        var runtime = await CreateAsync(databasePath, cancellationToken).ConfigureAwait(false);
        return runtime.Auth;
    }
}
