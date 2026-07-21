using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetViolations;

public sealed record ReportGetViolationsQuery(
    DateTime From,
    DateTime To) : IRequest<ViolationSummaryResponse>;

public sealed class ReportGetViolationsQueryHandler : IRequestHandler<ReportGetViolationsQuery, ViolationSummaryResponse>
{
    private readonly IReportService _service;

    public ReportGetViolationsQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<ViolationSummaryResponse> Handle(
        ReportGetViolationsQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetViolationsAsync(request.From, request.To, cancellationToken);
    }
}
