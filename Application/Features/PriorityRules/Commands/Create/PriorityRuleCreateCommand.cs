using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Commands.Create;

public sealed record PriorityRuleCreateCommand(
    CreatePriorityRuleRequest Request) : IRequest<PriorityRuleResponse>;

public sealed class PriorityRuleCreateCommandHandler : IRequestHandler<PriorityRuleCreateCommand, PriorityRuleResponse>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleCreateCommandHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public Task<PriorityRuleResponse> Handle(
        PriorityRuleCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
