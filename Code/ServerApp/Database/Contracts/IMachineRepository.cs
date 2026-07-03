using ServerApp.Database.Entities;

// Namespace chua cac interface truy cap du lieu database.
namespace ServerApp.Database.Contracts;

// Interface mo ta cac thao tac doc/cap nhat may tram.
public interface IMachineRepository
{
    // Lay danh sach tat ca may.
    Task<IReadOnlyList<MachineEntity>> ListAsync(CancellationToken cancellationToken = default);

    // Tim may theo ma may.
    Task<MachineEntity?> GetByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    // Cap nhat trang thai va thoi diem last seen cua may.
    Task UpdateStatusAsync(string machineId, string status, DateTime lastSeenUtc, CancellationToken cancellationToken = default);
}
