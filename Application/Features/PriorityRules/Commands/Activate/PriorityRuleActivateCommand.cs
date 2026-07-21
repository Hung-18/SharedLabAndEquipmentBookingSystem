using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Commands.Activate;

public sealed record PriorityRuleActivateCommand(
    int Id) : IRequest<bool>;

public sealed class PriorityRuleActivateCommandHandler : IRequestHandler<PriorityRuleActivateCommand, bool>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleActivateCommandHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        PriorityRuleActivateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return true;
    }
}
