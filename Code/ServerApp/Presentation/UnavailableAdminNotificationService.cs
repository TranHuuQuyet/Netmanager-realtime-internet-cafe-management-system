namespace ServerApp.Presentation;

public sealed class UnavailableAdminNotificationService : IAdminNotificationService
{
    public Task<AdminNotificationResult> SendAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AdminNotificationResult.ControlledError(
            request,
            "NOTIFICATION_SERVICE_UNAVAILABLE",
            "Admin notification is waiting for the runtime NOTIFICATION route."));
    }

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
