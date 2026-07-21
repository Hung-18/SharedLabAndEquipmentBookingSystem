using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Queries.GetUnreadByUserId;

public sealed record NotificationGetUnreadByUserIdQuery(
    int UserId) : IRequest<List<NotificationResponse>>;

public sealed class NotificationGetUnreadByUserIdQueryHandler : IRequestHandler<NotificationGetUnreadByUserIdQuery, List<NotificationResponse>>
{
    private readonly INotificationService _service;

    public NotificationGetUnreadByUserIdQueryHandler(INotificationService service)
    {
        _service = service;
    }

    public Task<List<NotificationResponse>> Handle(
        NotificationGetUnreadByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetUnreadByUserIdAsync(request.UserId, cancellationToken);
    }
}
