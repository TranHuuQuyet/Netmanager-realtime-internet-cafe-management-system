using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

// Quan ly vong doi session o tang nghiep vu: mo, dong, va lay session hien tai.
public interface ISessionService{
    // Tao session moi sau khi login hop le va tra ve thong tin session de client/server dung lai.
    Task<SessionInfo> OpenSessionAsync(UserRecord user, CancellationToken cancellationToken = default);

    // Dong session theo Id khi user logout hoac bi ngat.
    Task CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    // Lay session active hien tai cua user de kiem tra trang thai dang dang nhap.
    Task<SessionInfo?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default);
}
