using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAsRead;

public sealed record NotificationMarkAsReadCommand(
    int NotificationId) : IRequest<bool>;

public sealed class NotificationMarkAsReadCommandHandler : IRequestHandler<NotificationMarkAsReadCommand, bool>
{
    private readonly INotificationService _service;

    public NotificationMarkAsReadCommandHandler(INotificationService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        NotificationMarkAsReadCommand request,
        CancellationToken cancellationToken)
    {
        await _service.MarkAsReadAsync(request.NotificationId, cancellationToken);
        return true;
    }
}
