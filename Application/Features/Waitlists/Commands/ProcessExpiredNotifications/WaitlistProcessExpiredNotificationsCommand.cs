using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.ProcessExpiredNotifications;

public sealed record WaitlistProcessExpiredNotificationsCommand(
    DateTime ExpiredBefore) : IRequest<int>;

public sealed class WaitlistProcessExpiredNotificationsCommandHandler : IRequestHandler<WaitlistProcessExpiredNotificationsCommand, int>
{
    private readonly IWaitlistService _service;

    public WaitlistProcessExpiredNotificationsCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<int> Handle(
        WaitlistProcessExpiredNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ProcessExpiredNotificationsAsync(request.ExpiredBefore, cancellationToken);
    }
}
