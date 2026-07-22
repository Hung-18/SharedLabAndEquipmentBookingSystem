using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.Cancel;

public sealed record MaintenanceCancelCommand(
    int Id) : IRequest<bool>;

public sealed class MaintenanceCancelCommandHandler : IRequestHandler<MaintenanceCancelCommand, bool>
{
    private readonly IMaintenanceService _service;

    public MaintenanceCancelCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        MaintenanceCancelCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CancelAsync(request.Id, cancellationToken);
        return true;
    }
}
