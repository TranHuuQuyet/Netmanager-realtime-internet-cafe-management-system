namespace ServerApp.Presentation;

public sealed class UnavailableAdminChatService : IAdminChatService
{
    public event Action<AdminChatMessage>? MessageReceived
    {
        add { }
        remove { }
    }

    public Task<AdminChatResult> SendAsync(
        AdminChatRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AdminChatResult.ControlledError(
            request,
            "CHAT_SERVICE_UNAVAILABLE",
            "Admin chat is waiting for the runtime CHAT route."));
    }
}
