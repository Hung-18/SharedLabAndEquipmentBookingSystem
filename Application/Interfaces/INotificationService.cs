using Application.DTOs.Notifications;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponse>> GetByUserIdAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        Task<List<NotificationResponse>> GetUnreadByUserIdAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<UnreadNotificationCountResponse> CountUnreadAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<NotificationResponse> SendAsync(
            SendNotificationRequest request,
            CancellationToken cancellationToken);

        Task MarkAsReadAsync(
            int notificationId,
            CancellationToken cancellationToken);

        Task MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken);
    }
}
