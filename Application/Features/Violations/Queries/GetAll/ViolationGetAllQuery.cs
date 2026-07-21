using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetAll;

public sealed record ViolationGetAllQuery : IRequest<List<ViolationResponse>>;

public sealed class ViolationGetAllQueryHandler : IRequestHandler<ViolationGetAllQuery, List<ViolationResponse>>
{
    private readonly IViolationService _service;

    public ViolationGetAllQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<List<ViolationResponse>> Handle(
        ViolationGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
