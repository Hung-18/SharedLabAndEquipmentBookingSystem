using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Queries.GetByEquipmentId;

public sealed record MaintenanceGetByEquipmentIdQuery(
    int EquipmentId) : IRequest<List<MaintenanceResponse>>;

public sealed class MaintenanceGetByEquipmentIdQueryHandler : IRequestHandler<MaintenanceGetByEquipmentIdQuery, List<MaintenanceResponse>>
{
    private readonly IMaintenanceService _service;

    public MaintenanceGetByEquipmentIdQueryHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public Task<List<MaintenanceResponse>> Handle(
        MaintenanceGetByEquipmentIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByEquipmentIdAsync(request.EquipmentId, cancellationToken);
    }
}
