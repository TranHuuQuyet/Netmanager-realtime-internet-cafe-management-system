namespace ServerApp.Presentation;

public interface IAdminCommandService
{
    Task<AdminCommandResult> SendAsync(
        AdminCommandRequest request,
        CancellationToken cancellationToken = default);
}
