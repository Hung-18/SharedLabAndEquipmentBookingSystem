using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Queries.CountUnread;

public sealed record NotificationCountUnreadQuery(
    int UserId) : IRequest<UnreadNotificationCountResponse>;

public sealed class NotificationCountUnreadQueryHandler : IRequestHandler<NotificationCountUnreadQuery, UnreadNotificationCountResponse>
{
    private readonly INotificationService _service;

    public NotificationCountUnreadQueryHandler(INotificationService service)
    {
        _service = service;
    }

    public Task<UnreadNotificationCountResponse> Handle(
        NotificationCountUnreadQuery request,
        CancellationToken cancellationToken)
    {
        return _service.CountUnreadAsync(request.UserId, cancellationToken);
    }
}
