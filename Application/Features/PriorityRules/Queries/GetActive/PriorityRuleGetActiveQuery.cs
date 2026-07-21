using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Queries.GetActive;

public sealed record PriorityRuleGetActiveQuery : IRequest<List<PriorityRuleResponse>>;

public sealed class PriorityRuleGetActiveQueryHandler : IRequestHandler<PriorityRuleGetActiveQuery, List<PriorityRuleResponse>>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleGetActiveQueryHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public Task<List<PriorityRuleResponse>> Handle(
        PriorityRuleGetActiveQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetActiveAsync(cancellationToken);
    }
}
