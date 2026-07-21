using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Commands.Update;

public sealed record EquipmentUpdateCommand(
    int Id,
    UpdateEquipmentRequest Request) : IRequest<bool>;

public sealed class EquipmentUpdateCommandHandler : IRequestHandler<EquipmentUpdateCommand, bool>
{
    private readonly IEquipmentService _service;

    public EquipmentUpdateCommandHandler(IEquipmentService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        EquipmentUpdateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
