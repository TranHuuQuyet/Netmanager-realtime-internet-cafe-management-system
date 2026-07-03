using ServerApp.Database.Entities;

// Namespace gom cac hop dong va lop truy cap du lieu may tram.
namespace ServerApp.Database.Repositories;

// Interface dinh nghia cac thao tac du lieu lien quan den may trong quan net.
// Code nghiep vu chi can goi interface, con du lieu co the den tu seed, file, SQLite, SQL Server...
public interface IMachineRepository
{
    // Tim may theo ma may, vi du "MAY01" hoac "PC-01".
    // Ket qua co the null neu ma may khong ton tai.
    Task<MachineEntity?> FindByMachineIdAsync(string machineId, CancellationToken cancellationToken = default);

    // Cap nhat trang thai cua may va thoi diem gan nhat server nhin thay may do.
    // status co the la cac gia tri nhu Online, Offline, InUse tuy cach he thong quy uoc.
    Task UpdateStatusAsync(string machineId, string status, DateTime lastSeen, CancellationToken cancellationToken = default);
}
