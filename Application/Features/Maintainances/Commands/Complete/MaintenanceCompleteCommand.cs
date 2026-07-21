using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.Complete;

public sealed record MaintenanceCompleteCommand(
    int Id) : IRequest<bool>;

public sealed class MaintenanceCompleteCommandHandler : IRequestHandler<MaintenanceCompleteCommand, bool>
{
    private readonly IMaintenanceService _service;

    public MaintenanceCompleteCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        MaintenanceCompleteCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CompleteAsync(request.Id, cancellationToken);
        return true;
    }
}
