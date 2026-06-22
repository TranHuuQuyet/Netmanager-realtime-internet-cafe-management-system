namespace ServerApp.Presentation;

public interface IAdminChatService
{
    event Action<AdminChatMessage>? MessageReceived;

    Task<AdminChatResult> SendAsync(
        AdminChatRequest request,
        CancellationToken cancellationToken = default);
}
