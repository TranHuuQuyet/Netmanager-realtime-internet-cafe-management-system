using ServerApp.Auth.Contracts;
using ServerApp.Database;
using ServerApp.Database.Repositories;

namespace ServerApp.Auth.Services;

public sealed record AuthRuntime(
    NetManagerDatabase Database,
    IUserRepository Users,
    ISessionService Sessions,
    IAuthService Auth);

public static class AuthBootstrapper {
    public static async Task<AuthRuntime> CreateAsync(string? databasePath = null, CancellationToken cancellationToken = default) {
        var database = new NetManagerDatabase(new NetManagerDatabaseOptions(databasePath));
        await database.InitializeAsync(cancellationToken).ConfigureAwait(false);

        IUserRepository users = new SqliteUserRepository(database);
        ISessionRepository sessionRepository = new SqliteSessionRepository(database);
        ISessionService sessions = new SessionService(sessionRepository);
        IAuthService auth = new AuthService(users, sessions);

        return new AuthRuntime(database, users, sessions, auth);
    }
}
