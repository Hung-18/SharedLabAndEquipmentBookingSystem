using Application.DTOs.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponse>> GetByUserIdAsync(
            int actorUserId,
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        Task<List<NotificationResponse>> GetUnreadByUserIdAsync(
            int actorUserId,
            int userId,
            CancellationToken cancellationToken);

        Task<UnreadNotificationCountResponse> CountUnreadAsync(
            int actorUserId,
            int userId,
            CancellationToken cancellationToken);

        Task<NotificationResponse> SendAsync(
            SendNotificationRequest request,
            CancellationToken cancellationToken);

        Task MarkAsReadAsync(
            int notificationId,
            NotificationActionRequest request,
            CancellationToken cancellationToken);

        Task MarkAllAsReadAsync(
            int userId,
            NotificationActionRequest request,
            CancellationToken cancellationToken);
    }

}
