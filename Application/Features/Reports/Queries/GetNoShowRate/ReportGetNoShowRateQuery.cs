using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetNoShowRate;

public sealed record ReportGetNoShowRateQuery(
    DateTime From,
    DateTime To) : IRequest<NoShowRateResponse>;

public sealed class ReportGetNoShowRateQueryHandler : IRequestHandler<ReportGetNoShowRateQuery, NoShowRateResponse>
{
    private readonly IReportService _service;

    public ReportGetNoShowRateQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<NoShowRateResponse> Handle(
        ReportGetNoShowRateQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetNoShowRateAsync(request.From, request.To, cancellationToken);
    }
}
