// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Service fallback khi chat runtime chua duoc gan vao he thong network.
public sealed class UnavailableAdminChatService : IAdminChatService
{
    // Khong co runtime chat nen event nay khong luu subscriber nao.
    public event Action<AdminChatMessage>? MessageReceived
    {
        add { }
        remove { }
    }

    // Luon tra ve loi co kiem soat de UI hien thong bao thay vi crash.
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
