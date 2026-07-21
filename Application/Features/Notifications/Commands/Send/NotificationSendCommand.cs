using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Commands.Send;

public sealed record NotificationSendCommand(
    SendNotificationRequest Request) : IRequest<NotificationResponse>;

public sealed class NotificationSendCommandHandler : IRequestHandler<NotificationSendCommand, NotificationResponse>
{
    private readonly INotificationService _service;

    public NotificationSendCommandHandler(INotificationService service)
    {
        _service = service;
    }

    public Task<NotificationResponse> Handle(
        NotificationSendCommand request,
        CancellationToken cancellationToken)
    {
        return _service.SendAsync(request.Request, cancellationToken);
    }
}
