using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Queries.GetIncidentLogs;

public sealed record UsageLogGetIncidentLogsQuery(
    DateTime? From,
    DateTime? To) : IRequest<List<UsageLogResponse>>;

public sealed class UsageLogGetIncidentLogsQueryHandler : IRequestHandler<UsageLogGetIncidentLogsQuery, List<UsageLogResponse>>
{
    private readonly IUsageLogService _service;

    public UsageLogGetIncidentLogsQueryHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<List<UsageLogResponse>> Handle(
        UsageLogGetIncidentLogsQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetIncidentLogsAsync(request.From, request.To, cancellationToken);
    }
}
