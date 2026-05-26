using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

// Dung cho flow dang nhap: nhan request auth va tra ve ket qua da chuan hoa.
public interface IAuthService {
    // Xac thuc username/password/machineId va tra ve AuthResult co trang thai ro rang.
    Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default);
}