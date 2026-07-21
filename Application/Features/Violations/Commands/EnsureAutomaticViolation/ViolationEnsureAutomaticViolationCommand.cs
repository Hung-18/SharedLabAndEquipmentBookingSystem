using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Commands.EnsureAutomaticViolation;

public sealed record ViolationEnsureAutomaticViolationCommand(
    int BookingId,
    ViolationType ViolationType) : IRequest<ViolationResponse?>;

public sealed class ViolationEnsureAutomaticViolationCommandHandler : IRequestHandler<ViolationEnsureAutomaticViolationCommand, ViolationResponse?>
{
    private readonly IViolationService _service;

    public ViolationEnsureAutomaticViolationCommandHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<ViolationResponse?> Handle(
        ViolationEnsureAutomaticViolationCommand request,
        CancellationToken cancellationToken)
    {
        return _service.EnsureAutomaticViolationAsync(request.BookingId, request.ViolationType, cancellationToken);
    }
}
