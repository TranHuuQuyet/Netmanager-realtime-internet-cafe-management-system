using ServerApp.Database.Entities;

// Namespace gom cac lop repository cua tang Database.
// Repository la lop chiu trach nhiem doc/ghi du lieu cho mot nhom doi tuong.
namespace ServerApp.Database.Repositories;

// Lop nay luu session trong bo nho bang List, phu hop de seed/test nhanh.
// "sealed" nghia la lop nay khong cho lop khac ke thua.
public sealed class SeedSessionRepository
{
    // Danh sach session dang duoc giu tam trong RAM.
    // Khi tat chuong trinh thi du lieu trong danh sach nay se mat.
    private readonly List<SessionEntity> _sessions = [];

    // Kiem tra mot may co session dang hoat dong hay khong.
    // Tra ve Task<bool> de giong kieu async nhu khi lam voi database that.
    public Task<bool> HasActiveSessionForMachineAsync(string machineId, CancellationToken cancellationToken = default)
    {
        // Any tra ve true neu tim thay it nhat mot session thoa dieu kien.
        // So sanh MachineId khong phan biet chu hoa/chu thuong.
        // Status phai la "Active" thi moi duoc xem la session dang chay.
        var hasActiveSession = _sessions.Any(session =>
            string.Equals(session.MachineId, machineId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase));

        // Task.FromResult boc ket qua dong bo thanh Task de ham co dang async-compatible.
        return Task.FromResult(hasActiveSession);
    }

    // Tao mot session moi cho user tren mot may cu the.
    // Ham nhan userId, machineId va thoi diem bat dau session.
    public Task<SessionEntity> CreateAsync(Guid userId, string machineId, DateTime startTime, CancellationToken cancellationToken = default)
    {
        // Khoi tao doi tuong SessionEntity bang object initializer.
        // Guid.NewGuid() tao ma dinh danh duy nhat cho session moi.
        var session = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MachineId = machineId,
            Status = "Active",
            StartedAt = startTime,
            LastSeen = startTime
        };

        // Them session moi vao danh sach trong bo nho.
        _sessions.Add(session);

        // Tra ve session vua tao duoi dang Task.
        return Task.FromResult(session);
    }

    // Ket thuc mot session dua tren sessionId.
    // Neu tim thay session, ham doi Status thanh "Ended" va luu thoi diem ket thuc.
    public Task EndAsync(Guid sessionId, DateTime endTime, CancellationToken cancellationToken = default)
    {
        // FindIndex tra ve vi tri cua session trong List.
        // Neu khong tim thay thi tra ve -1.
        var index = _sessions.FindIndex(session => session.Id == sessionId);
        if (index >= 0)
        {
            // Record trong C# thuong bat bien theo cach dung init-only.
            // Cu phap "with" tao ban sao moi tu session cu va thay doi cac field can sua.
            _sessions[index] = _sessions[index] with { Status = "Ended", EndedAt = endTime };
        }

        // Task.CompletedTask dung khi ham khong can tra ve du lieu va da chay xong.
        return Task.CompletedTask;
    }
}
