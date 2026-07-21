using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.MarkBooked;

public sealed record WaitlistMarkBookedCommand(
    int Id) : IRequest<bool>;

public sealed class WaitlistMarkBookedCommandHandler : IRequestHandler<WaitlistMarkBookedCommand, bool>
{
    private readonly IWaitlistService _service;

    public WaitlistMarkBookedCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        WaitlistMarkBookedCommand request,
        CancellationToken cancellationToken)
    {
        await _service.MarkBookedAsync(request.Id, cancellationToken);
        return true;
    }
}
