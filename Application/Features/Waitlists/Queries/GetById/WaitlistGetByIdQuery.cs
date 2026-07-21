using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Queries.GetById;

public sealed record WaitlistGetByIdQuery(
    int Id) : IRequest<WaitlistResponse?>;

public sealed class WaitlistGetByIdQueryHandler : IRequestHandler<WaitlistGetByIdQuery, WaitlistResponse?>
{
    private readonly IWaitlistService _service;

    public WaitlistGetByIdQueryHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<WaitlistResponse?> Handle(
        WaitlistGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
