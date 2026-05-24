using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public interface ISessionRepository
{
    Task<bool> HasActiveSessionForMachineAsync(string machineId, CancellationToken cancellationToken = default);

    Task<SessionEntity> CreateAsync(Guid userId, string machineId, DateTime startTime, CancellationToken cancellationToken = default);

    Task EndAsync(Guid sessionId, DateTime endTime, CancellationToken cancellationToken = default);
}
