using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetDashboard;

public sealed record ReportGetDashboardQuery(
    DateTime From,
    DateTime To) : IRequest<DashboardResponse>;

public sealed class ReportGetDashboardQueryHandler : IRequestHandler<ReportGetDashboardQuery, DashboardResponse>
{
    private readonly IReportService _service;

    public ReportGetDashboardQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<DashboardResponse> Handle(
        ReportGetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetDashboardAsync(request.From, request.To, cancellationToken);
    }
}
