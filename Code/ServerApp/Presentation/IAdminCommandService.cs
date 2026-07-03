// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Interface gui lenh dieu khien may tu admin.
public interface IAdminCommandService
{
    // Gui mot command den client.
    Task<AdminCommandResult> SendAsync(
        AdminCommandRequest request,
        CancellationToken cancellationToken = default);
}
