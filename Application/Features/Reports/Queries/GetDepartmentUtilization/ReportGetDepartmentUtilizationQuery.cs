using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetDepartmentUtilization;

public sealed record ReportGetDepartmentUtilizationQuery(
    DateTime From,
    DateTime To) : IRequest<List<DepartmentUtilizationResponse>>;

public sealed class ReportGetDepartmentUtilizationQueryHandler : IRequestHandler<ReportGetDepartmentUtilizationQuery, List<DepartmentUtilizationResponse>>
{
    private readonly IReportService _service;

    public ReportGetDepartmentUtilizationQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<DepartmentUtilizationResponse>> Handle(
        ReportGetDepartmentUtilizationQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetDepartmentUtilizationAsync(request.From, request.To, cancellationToken);
    }
}
