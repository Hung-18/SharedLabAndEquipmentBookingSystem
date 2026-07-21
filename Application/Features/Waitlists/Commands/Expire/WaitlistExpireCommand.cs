using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.Expire;

public sealed record WaitlistExpireCommand(
    int Id) : IRequest<bool>;

public sealed class WaitlistExpireCommandHandler : IRequestHandler<WaitlistExpireCommand, bool>
{
    private readonly IWaitlistService _service;

    public WaitlistExpireCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        WaitlistExpireCommand request,
        CancellationToken cancellationToken)
    {
        await _service.ExpireAsync(request.Id, cancellationToken);
        return true;
    }
}
