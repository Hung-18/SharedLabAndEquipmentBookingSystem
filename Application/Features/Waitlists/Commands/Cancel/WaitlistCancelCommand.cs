using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.Cancel;

public sealed record WaitlistCancelCommand(
    int Id) : IRequest<bool>;

public sealed class WaitlistCancelCommandHandler : IRequestHandler<WaitlistCancelCommand, bool>
{
    private readonly IWaitlistService _service;

    public WaitlistCancelCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        WaitlistCancelCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CancelAsync(request.Id, cancellationToken);
        return true;
    }
}
