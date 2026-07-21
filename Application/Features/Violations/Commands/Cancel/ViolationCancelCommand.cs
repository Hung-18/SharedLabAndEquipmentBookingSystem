using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Commands.Cancel;

public sealed record ViolationCancelCommand(
    int Id) : IRequest<bool>;

public sealed class ViolationCancelCommandHandler : IRequestHandler<ViolationCancelCommand, bool>
{
    private readonly IViolationService _service;

    public ViolationCancelCommandHandler(IViolationService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        ViolationCancelCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CancelAsync(request.Id, cancellationToken);
        return true;
    }
}
