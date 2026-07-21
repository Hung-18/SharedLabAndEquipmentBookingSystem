using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Commands.Create;

public sealed record ViolationCreateCommand(
    CreateViolationRequest Request) : IRequest<ViolationResponse>;

public sealed class ViolationCreateCommandHandler : IRequestHandler<ViolationCreateCommand, ViolationResponse>
{
    private readonly IViolationService _service;

    public ViolationCreateCommandHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<ViolationResponse> Handle(
        ViolationCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
