// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Interface gui thong bao tu admin den client.
public interface IAdminNotificationService
{
    // Gui thong bao den mot may.
    Task<AdminNotificationResult> SendAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default);

    // Gui thong bao den nhieu may/pham vi rong hon.
    Task<AdminNotificationResult> BroadcastAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default);
}
