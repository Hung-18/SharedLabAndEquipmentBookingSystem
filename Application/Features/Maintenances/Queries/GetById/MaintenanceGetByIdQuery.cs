using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Queries.GetById;

public sealed record MaintenanceGetByIdQuery(
    int Id) : IRequest<MaintenanceDetailResponse?>;

public sealed class MaintenanceGetByIdQueryHandler : IRequestHandler<MaintenanceGetByIdQuery, MaintenanceDetailResponse?>
{
    private readonly IMaintenanceService _service;

    public MaintenanceGetByIdQueryHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public Task<MaintenanceDetailResponse?> Handle(
        MaintenanceGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
