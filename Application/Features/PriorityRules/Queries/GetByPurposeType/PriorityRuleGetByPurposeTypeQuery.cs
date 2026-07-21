using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Queries.GetByPurposeType;

public sealed record PriorityRuleGetByPurposeTypeQuery(
    BookingPurposeType PurposeType) : IRequest<PriorityRuleResponse?>;

public sealed class PriorityRuleGetByPurposeTypeQueryHandler : IRequestHandler<PriorityRuleGetByPurposeTypeQuery, PriorityRuleResponse?>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleGetByPurposeTypeQueryHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public Task<PriorityRuleResponse?> Handle(
        PriorityRuleGetByPurposeTypeQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByPurposeTypeAsync(request.PurposeType, cancellationToken);
    }
}
