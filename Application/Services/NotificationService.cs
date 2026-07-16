using Application.DTOs.Notifications;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly INotificationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        public NotificationService(
            INotificationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
        }

        public async Task<List<NotificationResponse>> GetByUserIdAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            EnsureReadAccess(actor, userId);
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

            var notifications = await _repository.GetByUserIdAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);
            return notifications.Select(MapResponse).ToList();
        }

        public async Task<List<NotificationResponse>> GetUnreadByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            EnsureReadAccess(actor, userId);
            var notifications = await _repository.GetUnreadByUserIdAsync(
                userId,
                cancellationToken);
            return notifications.Select(MapResponse).ToList();
        }

        public async Task<UnreadNotificationCountResponse> CountUnreadAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            EnsureReadAccess(actor, userId);
            return new UnreadNotificationCountResponse
            {
                UserId = userId,
                UnreadCount = await _repository.CountUnreadAsync(userId, cancellationToken)
            };
        }

        public async Task<NotificationResponse> SendAsync(
            SendNotificationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            if (actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được gửi thông báo thủ công.");

            var targetUser = await _unitOfWork.Users.GetUserByIdAsync(
                request.UserId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người nhận có ID {request.UserId}.");

            if (targetUser.Status is UserStatus.Inactive or UserStatus.Locked)
                throw new InvalidOperationException(
                    "Không thể gửi thông báo cho người dùng Inactive hoặc Locked.");
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Tiêu đề thông báo không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Nội dung thông báo không được để trống.");
            if (!Enum.IsDefined(request.NotificationType))
                throw new ArgumentException("Loại thông báo không hợp lệ.");

            var notification = new Notification(
                request.UserId,
                request.Title,
                request.Message,
                request.NotificationType);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _repository.AddAsync(notification, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _auditLogWriter.WriteAsync(
                    actor.UserId,
                    AuditActionType.Create,
                    nameof(Notification),
                    notification.NotificationId,
                    null,
                    new
                    {
                        notification.NotificationId,
                        notification.UserId,
                        notification.Title,
                        NotificationType = notification.NotificationType.ToString()
                    },
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return MapResponse(notification);
        }

        public async Task MarkAsReadAsync(
            int notificationId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            var notification = await _repository.GetByIdAsync(
                notificationId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy thông báo có ID {notificationId}.");
            EnsureReadAccess(actor, notification.UserId);
            if (notification.IsRead)
                return;

            notification.MarkAsRead();
            _repository.Update(notification);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(Notification),
                notification.NotificationId,
                new { IsRead = false },
                new { IsRead = true, Action = "MarkAsRead" },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedActorAsync(cancellationToken);
            EnsureReadAccess(actor, userId);
            int unreadCount = await _repository.CountUnreadAsync(userId, cancellationToken);
            if (unreadCount == 0)
                return;

            await _repository.MarkAllAsReadAsync(userId, cancellationToken);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(Notification),
                userId,
                new { UnreadCount = unreadCount },
                new { UnreadCount = 0, Action = "MarkAllAsRead" },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetAuthenticatedActorAsync(
            CancellationToken cancellationToken)
        {
            int actorUserId = _currentUserService.GetRequiredUserId();
            var actor = await _unitOfWork.Users.GetUserByIdAsync(
                actorUserId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {actorUserId}.");

            if (actor.Status is UserStatus.Inactive or UserStatus.Locked)
                throw new InvalidOperationException("Tài khoản không được phép thao tác.");
            return actor;
        }

        private static void EnsureReadAccess(User actor, int targetUserId)
        {
            if (actor.UserId == targetUserId || actor.Role?.RoleName == RoleName.Admin)
                return;
            throw new UnauthorizedAccessException(
                "Bạn chỉ được xem hoặc cập nhật thông báo của chính mình.");
        }

        private static NotificationResponse MapResponse(Notification notification)
        {
            return new NotificationResponse
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                NotificationType = notification.NotificationType.ToString(),
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}
