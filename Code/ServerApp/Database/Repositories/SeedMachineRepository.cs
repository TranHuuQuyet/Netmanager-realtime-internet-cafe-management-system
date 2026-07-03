using ServerApp.Database.Entities;

// Namespace gom cac lop repository cua tang Database.
namespace ServerApp.Database.Repositories;

// Repository may tram dung du lieu seed/in-memory.
// Lop nay giup code nghiep vu doc/ghi may ma chua can database that.
public sealed class SeedMachineRepository : IMachineRepository
{
    // Danh sach may duoc giu trong RAM.
    private readonly List<MachineEntity> _machines;

    // Constructor nhan danh sach may tuy chon.
    // Neu khong truyen machines, lop se lay du lieu mac dinh tu SeedData.Machines.
    public SeedMachineRepository(IEnumerable<MachineEntity>? machines = null)
    {
        _machines = (machines ?? SeedData.Machines).ToList();
    }

    // Tim mot may theo machineId.
    public Task<MachineEntity?> FindByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
    {
        // Tim may dau tien co MachineId trung voi tham so dau vao, khong phan biet hoa thuong.
        var machine = _machines.FirstOrDefault(candidate =>
            string.Equals(candidate.MachineId, machineId, StringComparison.OrdinalIgnoreCase));

        // Tra ket qua duoi dang Task de giong repository async.
        return Task.FromResult(machine);
    }

    // Cap nhat trang thai va LastSeen cua may.
    public Task UpdateStatusAsync(string machineId, string status, DateTime lastSeen, CancellationToken cancellationToken = default)
    {
        // Tim vi tri may trong danh sach theo machineId.
        var index = _machines.FindIndex(machine =>
            string.Equals(machine.MachineId, machineId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            // Vi MachineEntity la record, dung "with" de tao ban sao moi voi Status/LastSeen moi.
            _machines[index] = _machines[index] with { Status = status, LastSeen = lastSeen };
        }

        // Ham hoan tat ma khong tra ve du lieu.
        return Task.CompletedTask;
    }
}
