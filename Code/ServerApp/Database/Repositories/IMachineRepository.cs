using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public interface IMachineRepository
{
    Task<MachineEntity?> FindByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string machineId, string status, DateTime lastSeen, CancellationToken cancellationToken = default);
}
