using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.Create;

public sealed record WaitlistCreateCommand(
    CreateWaitlistRequest Request) : IRequest<WaitlistResponse>;

public sealed class WaitlistCreateCommandHandler : IRequestHandler<WaitlistCreateCommand, WaitlistResponse>
{
    private readonly IWaitlistService _service;

    public WaitlistCreateCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<WaitlistResponse> Handle(
        WaitlistCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
