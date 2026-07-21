using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Queries.GetAll;

public sealed record EquipmentGetAllQuery : IRequest<List<EquipmentResponse>>;

public sealed class EquipmentGetAllQueryHandler : IRequestHandler<EquipmentGetAllQuery, List<EquipmentResponse>>
{
    private readonly IEquipmentService _service;

    public EquipmentGetAllQueryHandler(IEquipmentService service)
    {
        _service = service;
    }

    public Task<List<EquipmentResponse>> Handle(
        EquipmentGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
