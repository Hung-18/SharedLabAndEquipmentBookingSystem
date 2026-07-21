using Application.DTOs.PriorityRules;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.PriorityRules.Commands.Deactivate;

public sealed record PriorityRuleDeactivateCommand(
    int Id) : IRequest<bool>;

public sealed class PriorityRuleDeactivateCommandHandler : IRequestHandler<PriorityRuleDeactivateCommand, bool>
{
    private readonly IPriorityRuleService _service;

    public PriorityRuleDeactivateCommandHandler(IPriorityRuleService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        PriorityRuleDeactivateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return true;
    }
}
