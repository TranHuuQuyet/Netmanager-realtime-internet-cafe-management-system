using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public sealed class SeedSessionRepository
{
    private readonly List<SessionEntity> _sessions = [];

    public Task<bool> HasActiveSessionForMachineAsync(string machineId, CancellationToken cancellationToken = default)
    {
        var hasActiveSession = _sessions.Any(session =>
            string.Equals(session.MachineId, machineId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(hasActiveSession);
    }

    public Task<SessionEntity> CreateAsync(Guid userId, string machineId, DateTime startTime, CancellationToken cancellationToken = default)
    {
        var session = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MachineId = machineId,
            Status = "Active",
            StartedAt = startTime,
            LastSeen = startTime
        };

        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task EndAsync(Guid sessionId, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var index = _sessions.FindIndex(session => session.Id == sessionId);
        if (index >= 0)
        {
            _sessions[index] = _sessions[index] with { Status = "Ended", EndedAt = endTime };
        }

        return Task.CompletedTask;
    }
}
