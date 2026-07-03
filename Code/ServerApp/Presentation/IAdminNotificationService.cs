namespace ServerApp.Presentation;

public interface IAdminNotificationService
{
    Task<AdminNotificationResult> SendAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminNotificationResult> BroadcastAsync(
        AdminNotificationRequest request,
        CancellationToken cancellationToken = default);
}
