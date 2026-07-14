using Application.DTOs.Notifications;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(
            INotificationRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<NotificationResponse>> GetByUserIdAsync(
            int actorUserId,
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            await ValidateReadAccessAsync(
                actorUserId,
                userId,
                cancellationToken);

            pageNumber = pageNumber <= 0
                ? 1
                : pageNumber;

            pageSize = pageSize <= 0
                ? DefaultPageSize
                : Math.Min(pageSize, MaxPageSize);

            var notifications =
                await _repository.GetByUserIdAsync(
                    userId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            return notifications
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<NotificationResponse>> GetUnreadByUserIdAsync(
            int actorUserId,
            int userId,
            CancellationToken cancellationToken)
        {
            await ValidateReadAccessAsync(
                actorUserId,
                userId,
                cancellationToken);

            var notifications =
                await _repository.GetUnreadByUserIdAsync(
                    userId,
                    cancellationToken);

            return notifications
                .Select(MapResponse)
                .ToList();
        }

        public async Task<UnreadNotificationCountResponse> CountUnreadAsync(
            int actorUserId,
            int userId,
            CancellationToken cancellationToken)
        {
            await ValidateReadAccessAsync(
                actorUserId,
                userId,
                cancellationToken);

            int unreadCount =
                await _repository.CountUnreadAsync(
                    userId,
                    cancellationToken);

            return new UnreadNotificationCountResponse
            {
                UserId = userId,
                UnreadCount = unreadCount
            };
        }

        public async Task<NotificationResponse> SendAsync(
            SendNotificationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var targetUser =
                await _unitOfWork.Users.GetUserByIdAsync(
                    request.UserId,
                    cancellationToken);

            if (targetUser is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người nhận có ID {request.UserId}.");
            }

            if (targetUser.Status == UserStatus.Inactive
                || targetUser.Status == UserStatus.Locked)
            {
                throw new InvalidOperationException(
                    "Không thể gửi thông báo cho người dùng đang Inactive hoặc Locked.");
            }

            if (!Enum.IsDefined(
                    typeof(NotificationType),
                    request.NotificationType))
            {
                throw new ArgumentException(
                    "Loại thông báo không hợp lệ.");
            }

            var notification = new Notification(
                request.UserId,
                request.Title,
                request.Message,
                request.NotificationType);

            await _repository.AddAsync(
                notification,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(notification);
        }

        public async Task MarkAsReadAsync(
            int notificationId,
            NotificationActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var notification =
                await _repository.GetByIdAsync(
                    notificationId,
                    cancellationToken);

            if (notification is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy thông báo có ID {notificationId}.");
            }

            await ValidateReadAccessAsync(
                request.ActorUserId,
                notification.UserId,
                cancellationToken);

            notification.MarkAsRead();

            _repository.Update(notification);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task MarkAllAsReadAsync(
            int userId,
            NotificationActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateReadAccessAsync(
                request.ActorUserId,
                userId,
                cancellationToken);

            await _repository.MarkAllAsReadAsync(
                userId,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task ValidateReadAccessAsync(
            int actorUserId,
            int targetUserId,
            CancellationToken cancellationToken)
        {
            var targetUser =
                await _unitOfWork.Users.GetUserByIdAsync(
                    targetUserId,
                    cancellationToken);

            if (targetUser is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {targetUserId}.");
            }

            var actor =
                await _unitOfWork.Users.GetUserByIdAsync(
                    actorUserId,
                    cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người thực hiện có ID {actorUserId}.");
            }

            if (actor.Status != UserStatus.Active
                && actorUserId != targetUserId)
            {
                throw new InvalidOperationException(
                    "Người thực hiện hiện không ở trạng thái Active.");
            }

            bool isOwner =
                actorUserId == targetUserId;

            bool isAdmin =
                actor.Role?.RoleName == RoleName.Admin;

            if (!isOwner && !isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ chủ thông báo hoặc Admin được xem và cập nhật thông báo.");
            }
        }

        private async Task ValidateAdminAsync(
            int actorUserId,
            CancellationToken cancellationToken)
        {
            var actor =
                await _unitOfWork.Users.GetUserByIdAsync(
                    actorUserId,
                    cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người thực hiện có ID {actorUserId}.");
            }

            if (actor.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Người thực hiện hiện không ở trạng thái Active.");
            }

            if (actor.Role?.RoleName != RoleName.Admin)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được gửi thông báo thủ công.");
            }
        }

        private static NotificationResponse MapResponse(
            Notification notification)
        {
            return new NotificationResponse
            {
                NotificationId =
                    notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                NotificationType =
                    notification.NotificationType.ToString(),
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }

}
