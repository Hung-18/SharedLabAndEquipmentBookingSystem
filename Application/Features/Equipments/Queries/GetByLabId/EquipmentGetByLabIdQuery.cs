using Application.DTOs.Equipments;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Equipments.Queries.GetByLabId;

public sealed record EquipmentGetByLabIdQuery(
    int LabId) : IRequest<List<EquipmentResponse>>;

public sealed class EquipmentGetByLabIdQueryHandler : IRequestHandler<EquipmentGetByLabIdQuery, List<EquipmentResponse>>
{
    private readonly IEquipmentService _service;

    public EquipmentGetByLabIdQueryHandler(IEquipmentService service)
    {
        _service = service;
    }

    public Task<List<EquipmentResponse>> Handle(
        EquipmentGetByLabIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByLabIdAsync(request.LabId, cancellationToken);
    }
}
