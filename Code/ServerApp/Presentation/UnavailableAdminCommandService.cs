namespace ServerApp.Presentation;

public sealed class UnavailableAdminCommandService : IAdminCommandService
{
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
