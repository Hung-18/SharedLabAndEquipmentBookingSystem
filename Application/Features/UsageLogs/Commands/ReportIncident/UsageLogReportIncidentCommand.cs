using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Commands.ReportIncident;

public sealed record UsageLogReportIncidentCommand(
    int LogId,
    ReportUsageIncidentRequest Request) : IRequest<UsageLogResponse>;

public sealed class UsageLogReportIncidentCommandHandler : IRequestHandler<UsageLogReportIncidentCommand, UsageLogResponse>
{
    private readonly IUsageLogService _service;

    public UsageLogReportIncidentCommandHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse> Handle(
        UsageLogReportIncidentCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ReportIncidentAsync(request.LogId, request.Request, cancellationToken);
    }
}
