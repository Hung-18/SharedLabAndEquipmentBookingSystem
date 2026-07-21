using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Commands.Update;

public sealed record PriorityRuleUpdateCommand(
    int Id,
    UpdatePriorityRuleRequest Request) : IRequest<bool>;

public sealed class PriorityRuleUpdateCommandHandler : IRequestHandler<PriorityRuleUpdateCommand, bool>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleUpdateCommandHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        PriorityRuleUpdateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
