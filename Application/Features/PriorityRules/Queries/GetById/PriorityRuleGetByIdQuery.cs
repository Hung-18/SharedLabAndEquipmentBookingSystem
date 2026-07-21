using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Queries.GetById;

public sealed record PriorityRuleGetByIdQuery(
    int Id) : IRequest<PriorityRuleResponse?>;

public sealed class PriorityRuleGetByIdQueryHandler : IRequestHandler<PriorityRuleGetByIdQuery, PriorityRuleResponse?>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleGetByIdQueryHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public Task<PriorityRuleResponse?> Handle(
        PriorityRuleGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
