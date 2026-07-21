using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Queries.GetByUserId;

public sealed record WaitlistGetByUserIdQuery(
    int UserId) : IRequest<List<WaitlistResponse>>;

public sealed class WaitlistGetByUserIdQueryHandler : IRequestHandler<WaitlistGetByUserIdQuery, List<WaitlistResponse>>
{
    private readonly IWaitlistService _service;

    public WaitlistGetByUserIdQueryHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<List<WaitlistResponse>> Handle(
        WaitlistGetByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
