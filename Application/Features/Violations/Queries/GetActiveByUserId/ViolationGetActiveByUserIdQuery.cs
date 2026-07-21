using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetActiveByUserId;

public sealed record ViolationGetActiveByUserIdQuery(
    int UserId) : IRequest<List<ViolationResponse>>;

public sealed class ViolationGetActiveByUserIdQueryHandler : IRequestHandler<ViolationGetActiveByUserIdQuery, List<ViolationResponse>>
{
    private readonly IViolationService _service;

    public ViolationGetActiveByUserIdQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<List<ViolationResponse>> Handle(
        ViolationGetActiveByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetActiveByUserIdAsync(request.UserId, cancellationToken);
    }
}
