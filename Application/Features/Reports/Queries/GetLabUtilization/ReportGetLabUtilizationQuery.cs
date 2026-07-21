using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetLabUtilization;

public sealed record ReportGetLabUtilizationQuery(
    DateTime From,
    DateTime To) : IRequest<List<ResourceUtilizationResponse>>;

public sealed class ReportGetLabUtilizationQueryHandler : IRequestHandler<ReportGetLabUtilizationQuery, List<ResourceUtilizationResponse>>
{
    private readonly IReportService _service;

    public ReportGetLabUtilizationQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<ResourceUtilizationResponse>> Handle(
        ReportGetLabUtilizationQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetLabUtilizationAsync(request.From, request.To, cancellationToken);
    }
}
