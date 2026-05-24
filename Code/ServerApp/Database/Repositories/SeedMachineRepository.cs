using ServerApp.Database.Entities;

namespace ServerApp.Database.Repositories;

public sealed class SeedMachineRepository : IMachineRepository
{
    private readonly List<MachineEntity> _machines;

    public SeedMachineRepository(IEnumerable<MachineEntity>? machines = null)
    {
        _machines = (machines ?? SeedData.Machines).ToList();
    }

    public Task<MachineEntity?> FindByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        var machine = _machines.FirstOrDefault(candidate =>
            string.Equals(candidate.MachineId, machineId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(machine);
    }

    public Task UpdateStatusAsync(string machineId, string status, DateTime lastSeen, CancellationToken cancellationToken = default)
    {
        var index = _machines.FindIndex(machine =>
            string.Equals(machine.MachineId, machineId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            _machines[index] = _machines[index] with { Status = status, LastSeen = lastSeen };
        }

        return Task.CompletedTask;
    }
}
