using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetById;

public sealed record ViolationGetByIdQuery(
    int Id) : IRequest<ViolationResponse?>;

public sealed class ViolationGetByIdQueryHandler : IRequestHandler<ViolationGetByIdQuery, ViolationResponse?>
{
    private readonly IViolationService _service;

    public ViolationGetByIdQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<ViolationResponse?> Handle(
        ViolationGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
