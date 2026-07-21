using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Queries.GetAll;

public sealed record MaintenanceGetAllQuery : IRequest<List<MaintenanceResponse>>;

public sealed class MaintenanceGetAllQueryHandler : IRequestHandler<MaintenanceGetAllQuery, List<MaintenanceResponse>>
{
    private readonly IMaintenanceService _service;

    public MaintenanceGetAllQueryHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public Task<List<MaintenanceResponse>> Handle(
        MaintenanceGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
