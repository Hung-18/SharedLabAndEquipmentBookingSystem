using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Queries.GetAll;

public sealed record WaitlistGetAllQuery : IRequest<List<WaitlistResponse>>;

public sealed class WaitlistGetAllQueryHandler : IRequestHandler<WaitlistGetAllQuery, List<WaitlistResponse>>
{
    private readonly IWaitlistService _service;

    public WaitlistGetAllQueryHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<List<WaitlistResponse>> Handle(
        WaitlistGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
