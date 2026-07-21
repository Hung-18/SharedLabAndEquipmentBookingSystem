using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetUsageTrend;

public sealed record ReportGetUsageTrendQuery(
    DateTime From,
    DateTime To,
    string GroupBy) : IRequest<List<UsageTrendResponse>>;

public sealed class ReportGetUsageTrendQueryHandler : IRequestHandler<ReportGetUsageTrendQuery, List<UsageTrendResponse>>
{
    private readonly IReportService _service;

    public ReportGetUsageTrendQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<UsageTrendResponse>> Handle(
        ReportGetUsageTrendQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetUsageTrendAsync(request.From, request.To, request.GroupBy, cancellationToken);
    }
}
