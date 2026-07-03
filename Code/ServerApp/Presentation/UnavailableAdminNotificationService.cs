// Namespace cua tang Presentation: cac model/service cho UI admin.
namespace ServerApp.Presentation;

// Service fallback khi notification runtime chua duoc gan vao network route.
public sealed class UnavailableAdminNotificationService : IAdminNotificationService
{
    // Gui mot may nhung service chua san sang nen tra ve loi co kiem soat.
    public Task<AdminNotificationResult> SendAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AdminNotificationResult.ControlledError(
            request,
            "NOTIFICATION_SERVICE_UNAVAILABLE",
            "Admin notification is waiting for the runtime NOTIFICATION route."));
    }

    // Broadcast cung tra ve loi vi notification route chua san sang.
    public Task<AdminNotificationResult> BroadcastAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AdminNotificationResult.ControlledError(
            request,
            "NOTIFICATION_SERVICE_UNAVAILABLE",
            "Admin notification is waiting for the runtime NOTIFICATION route."));
    }
}
