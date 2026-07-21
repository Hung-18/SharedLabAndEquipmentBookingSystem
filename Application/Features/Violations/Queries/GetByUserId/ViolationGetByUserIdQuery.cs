using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetByUserId;

public sealed record ViolationGetByUserIdQuery(
    int UserId) : IRequest<List<ViolationResponse>>;

public sealed class ViolationGetByUserIdQueryHandler : IRequestHandler<ViolationGetByUserIdQuery, List<ViolationResponse>>
{
    private readonly IViolationService _service;

    public ViolationGetByUserIdQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<List<ViolationResponse>> Handle(
        ViolationGetByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
