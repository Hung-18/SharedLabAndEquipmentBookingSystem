using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Commands.Resolve;

public sealed record ViolationResolveCommand(
    int Id) : IRequest<bool>;

public sealed class ViolationResolveCommandHandler : IRequestHandler<ViolationResolveCommand, bool>
{
    private readonly IViolationService _service;

    public ViolationResolveCommandHandler(IViolationService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        ViolationResolveCommand request,
        CancellationToken cancellationToken)
    {
        await _service.ResolveAsync(request.Id, cancellationToken);
        return true;
    }
}
