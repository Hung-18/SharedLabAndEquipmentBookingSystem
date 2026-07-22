using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.Start;

public sealed record MaintenanceStartCommand(
    int Id) : IRequest<bool>;

public sealed class MaintenanceStartCommandHandler : IRequestHandler<MaintenanceStartCommand, bool>
{
    private readonly IMaintenanceService _service;

    public MaintenanceStartCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        MaintenanceStartCommand request,
        CancellationToken cancellationToken)
    {
        await _service.StartAsync(request.Id, cancellationToken);
        return true;
    }
}
