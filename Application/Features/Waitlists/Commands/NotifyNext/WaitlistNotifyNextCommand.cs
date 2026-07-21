using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.NotifyNext;

public sealed record WaitlistNotifyNextCommand(
    NotifyNextWaitlistRequest Request) : IRequest<WaitlistResponse>;

public sealed class WaitlistNotifyNextCommandHandler : IRequestHandler<WaitlistNotifyNextCommand, WaitlistResponse>
{
    private readonly IWaitlistService _service;

    public WaitlistNotifyNextCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<WaitlistResponse> Handle(
        WaitlistNotifyNextCommand request,
        CancellationToken cancellationToken)
    {
        return _service.NotifyNextAsync(request.Request, cancellationToken);
    }
}
