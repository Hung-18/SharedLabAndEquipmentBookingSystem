using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Commands.Delete;

public sealed record EquipmentDeleteCommand(
    int Id) : IRequest<bool>;

public sealed class EquipmentDeleteCommandHandler : IRequestHandler<EquipmentDeleteCommand, bool>
{
    private readonly IEquipmentService _service;

    public EquipmentDeleteCommandHandler(IEquipmentService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        EquipmentDeleteCommand request,
        CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
