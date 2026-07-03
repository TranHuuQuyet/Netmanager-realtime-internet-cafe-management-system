// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Interface gui/nhan chat giua admin va client.
public interface IAdminChatService
{
    // Event khi co tin nhan moi gui den UI admin.
    event Action<AdminChatMessage>? MessageReceived;

    // Gui tin nhan chat den mot may.
    Task<AdminChatResult> SendAsync(
        AdminChatRequest request,
        CancellationToken cancellationToken = default);
}
