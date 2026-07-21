using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Commands.RejectIncident;

public sealed record UsageLogRejectIncidentCommand(
    int LogId,
    ReviewUsageIncidentRequest Request) : IRequest<UsageLogResponse>;

public sealed class UsageLogRejectIncidentCommandHandler : IRequestHandler<UsageLogRejectIncidentCommand, UsageLogResponse>
{
    private readonly IUsageLogService _service;

    public UsageLogRejectIncidentCommandHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse> Handle(
        UsageLogRejectIncidentCommand request,
        CancellationToken cancellationToken)
    {
        return _service.RejectIncidentAsync(request.LogId, request.Request, cancellationToken);
    }
}
