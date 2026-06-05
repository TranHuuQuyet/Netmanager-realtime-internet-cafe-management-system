using ServerApp.Database.Contracts;

namespace ServerApp.Database;

public sealed record DatabaseRuntime(
    IUserRepository Users,
    ISessionRepository Sessions,
    IMachineRepository Machines);
