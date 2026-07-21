using Application.DTOs.Notifications;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed record NotificationMarkAllAsReadCommand(
    int UserId) : IRequest<bool>;

public sealed class NotificationMarkAllAsReadCommandHandler : IRequestHandler<NotificationMarkAllAsReadCommand, bool>
{
    private readonly INotificationService _service;

    public NotificationMarkAllAsReadCommandHandler(INotificationService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        NotificationMarkAllAsReadCommand request,
        CancellationToken cancellationToken)
    {
        await _service.MarkAllAsReadAsync(request.UserId, cancellationToken);
        return true;
    }
}
