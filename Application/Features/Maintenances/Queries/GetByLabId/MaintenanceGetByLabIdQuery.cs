using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Queries.GetByLabId;

public sealed record MaintenanceGetByLabIdQuery(
    int LabId) : IRequest<List<MaintenanceResponse>>;

public sealed class MaintenanceGetByLabIdQueryHandler : IRequestHandler<MaintenanceGetByLabIdQuery, List<MaintenanceResponse>>
{
    private readonly IMaintenanceService _service;

    public MaintenanceGetByLabIdQueryHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public Task<List<MaintenanceResponse>> Handle(
        MaintenanceGetByLabIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByLabIdAsync(request.LabId, cancellationToken);
    }
}
