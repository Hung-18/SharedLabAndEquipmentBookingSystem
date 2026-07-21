using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Queries.GetById;

public sealed record EquipmentGetByIdQuery(
    int Id) : IRequest<EquipmentDetailResponse?>;

public sealed class EquipmentGetByIdQueryHandler : IRequestHandler<EquipmentGetByIdQuery, EquipmentDetailResponse?>
{
    private readonly IEquipmentService _service;

    public EquipmentGetByIdQueryHandler(IEquipmentService service)
    {
        _service = service;
    }

    public Task<EquipmentDetailResponse?> Handle(
        EquipmentGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
