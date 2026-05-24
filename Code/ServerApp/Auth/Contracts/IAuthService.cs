using ServerApp.Auth.Models;

namespace ServerApp.Auth.Contracts;

public interface IAuthService {
    Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default);
}
