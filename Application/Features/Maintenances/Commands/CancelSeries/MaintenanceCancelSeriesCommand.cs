using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.CancelSeries;

public sealed record MaintenanceCancelSeriesCommand(
    int Id) : IRequest<bool>;

public sealed class MaintenanceCancelSeriesCommandHandler : IRequestHandler<MaintenanceCancelSeriesCommand, bool>
{
    private readonly IMaintenanceService _service;

    public MaintenanceCancelSeriesCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        MaintenanceCancelSeriesCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CancelSeriesAsync(request.Id, cancellationToken);
        return true;
    }
}
