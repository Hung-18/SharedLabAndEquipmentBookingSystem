using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetEquipmentUtilization;

public sealed record ReportGetEquipmentUtilizationQuery(
    DateTime From,
    DateTime To) : IRequest<List<ResourceUtilizationResponse>>;

public sealed class ReportGetEquipmentUtilizationQueryHandler : IRequestHandler<ReportGetEquipmentUtilizationQuery, List<ResourceUtilizationResponse>>
{
    private readonly IReportService _service;

    public ReportGetEquipmentUtilizationQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<ResourceUtilizationResponse>> Handle(
        ReportGetEquipmentUtilizationQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetEquipmentUtilizationAsync(request.From, request.To, cancellationToken);
    }
}
