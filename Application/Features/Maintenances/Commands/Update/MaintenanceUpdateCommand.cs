using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.Update;

public sealed record MaintenanceUpdateCommand(
    int Id,
    UpdateMaintenanceRequest Request) : IRequest<bool>;

public sealed class MaintenanceUpdateCommandHandler : IRequestHandler<MaintenanceUpdateCommand, bool>
{
    private readonly IMaintenanceService _service;

    public MaintenanceUpdateCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        MaintenanceUpdateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
