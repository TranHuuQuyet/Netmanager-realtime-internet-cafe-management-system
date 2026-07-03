// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Service fallback khi command runtime chua duoc gan vao network route.
public sealed class UnavailableAdminCommandService : IAdminCommandService
{
    // Luon tra ve loi co kiem soat de UI biet command service chua san sang.
    public Task<AdminCommandResult> SendAsync(
        AdminCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AdminCommandResult.ControlledError(
            request,
            "COMMAND_SERVICE_UNAVAILABLE",
            "Admin command service is not bound to the runtime network route."));
    }
}
