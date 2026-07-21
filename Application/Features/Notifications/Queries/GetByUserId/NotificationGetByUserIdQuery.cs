using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Queries.GetByUserId;

public sealed record NotificationGetByUserIdQuery(
    int UserId,
    int PageNumber,
    int PageSize) : IRequest<List<NotificationResponse>>;

public sealed class NotificationGetByUserIdQueryHandler : IRequestHandler<NotificationGetByUserIdQuery, List<NotificationResponse>>
{
    private readonly INotificationService _service;

    public NotificationGetByUserIdQueryHandler(INotificationService service)
    {
        _service = service;
    }

    public Task<List<NotificationResponse>> Handle(
        NotificationGetByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByUserIdAsync(request.UserId, request.PageNumber, request.PageSize, cancellationToken);
    }
}
