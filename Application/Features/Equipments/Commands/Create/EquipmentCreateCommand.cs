using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Commands.Create;

public sealed record EquipmentCreateCommand(
    CreateEquipmentRequest Request) : IRequest<EquipmentDetailResponse>;

public sealed class EquipmentCreateCommandHandler : IRequestHandler<EquipmentCreateCommand, EquipmentDetailResponse>
{
    private readonly IEquipmentService _service;

    public EquipmentCreateCommandHandler(IEquipmentService service)
    {
        _service = service;
    }

    public Task<EquipmentDetailResponse> Handle(
        EquipmentCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
