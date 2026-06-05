using ServerApp.Database.Entities;

namespace ServerApp.Database.Contracts;

public interface IMachineRepository
{
    Task<MachineEntity?> GetByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string machineId, string status, DateTime lastSeenUtc, CancellationToken cancellationToken = default);
}
