using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetMaintenanceCostsByLab;

public sealed record ReportGetMaintenanceCostsByLabQuery(
    DateTime From,
    DateTime To) : IRequest<List<MaintenanceCostResponse>>;

public sealed class ReportGetMaintenanceCostsByLabQueryHandler : IRequestHandler<ReportGetMaintenanceCostsByLabQuery, List<MaintenanceCostResponse>>
{
    private readonly IReportService _service;

    public ReportGetMaintenanceCostsByLabQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<MaintenanceCostResponse>> Handle(
        ReportGetMaintenanceCostsByLabQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetMaintenanceCostsByLabAsync(request.From, request.To, cancellationToken);
    }
}
