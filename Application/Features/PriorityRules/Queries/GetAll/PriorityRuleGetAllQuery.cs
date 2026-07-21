using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Queries.GetAll;

public sealed record PriorityRuleGetAllQuery : IRequest<List<PriorityRuleResponse>>;

public sealed class PriorityRuleGetAllQueryHandler : IRequestHandler<PriorityRuleGetAllQuery, List<PriorityRuleResponse>>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleGetAllQueryHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public Task<List<PriorityRuleResponse>> Handle(
        PriorityRuleGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
