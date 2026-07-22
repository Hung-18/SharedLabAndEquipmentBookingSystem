using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Maintenances.Commands.Create;

public sealed record MaintenanceCreateCommand(
    CreateMaintenanceRequest Request) : IRequest<MaintenanceDetailResponse>;

public sealed class MaintenanceCreateCommandHandler : IRequestHandler<MaintenanceCreateCommand, MaintenanceDetailResponse>
{
    private readonly IMaintenanceService _service;

    public MaintenanceCreateCommandHandler(IMaintenanceService service)
    {
        _service = service;
    }

    public Task<MaintenanceDetailResponse> Handle(
        MaintenanceCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
