using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetMaintenanceCostsByEquipment;

public sealed record ReportGetMaintenanceCostsByEquipmentQuery(
    DateTime From,
    DateTime To) : IRequest<List<MaintenanceCostResponse>>;

public sealed class ReportGetMaintenanceCostsByEquipmentQueryHandler : IRequestHandler<ReportGetMaintenanceCostsByEquipmentQuery, List<MaintenanceCostResponse>>
{
    private readonly IReportService _service;

    public ReportGetMaintenanceCostsByEquipmentQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<MaintenanceCostResponse>> Handle(
        ReportGetMaintenanceCostsByEquipmentQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetMaintenanceCostsByEquipmentAsync(request.From, request.To, cancellationToken);
    }
}
