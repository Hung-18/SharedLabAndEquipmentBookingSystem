using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Commands.ConfirmIncident;

public sealed record UsageLogConfirmIncidentCommand(
    int LogId,
    ReviewUsageIncidentRequest Request) : IRequest<UsageLogResponse>;

public sealed class UsageLogConfirmIncidentCommandHandler : IRequestHandler<UsageLogConfirmIncidentCommand, UsageLogResponse>
{
    private readonly IUsageLogService _service;

    public UsageLogConfirmIncidentCommandHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse> Handle(
        UsageLogConfirmIncidentCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ConfirmIncidentAsync(request.LogId, request.Request, cancellationToken);
    }
}
