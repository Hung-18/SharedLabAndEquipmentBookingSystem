using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetMaintenanceHistory;

public sealed record ReportGetMaintenanceHistoryQuery(
    MaintenanceHistoryQueryRequest Request) : IRequest<PagedMaintenanceHistoryResponse>;

public sealed class ReportGetMaintenanceHistoryQueryHandler : IRequestHandler<ReportGetMaintenanceHistoryQuery, PagedMaintenanceHistoryResponse>
{
    private readonly IReportService _service;

    public ReportGetMaintenanceHistoryQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<PagedMaintenanceHistoryResponse> Handle(
        ReportGetMaintenanceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetMaintenanceHistoryAsync(request.Request, cancellationToken);
    }
}
