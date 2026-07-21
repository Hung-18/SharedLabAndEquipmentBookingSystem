using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetBookingsByStatus;

public sealed record ReportGetBookingsByStatusQuery(
    DateTime From,
    DateTime To) : IRequest<List<CategoryCountResponse>>;

public sealed class ReportGetBookingsByStatusQueryHandler : IRequestHandler<ReportGetBookingsByStatusQuery, List<CategoryCountResponse>>
{
    private readonly IReportService _service;

    public ReportGetBookingsByStatusQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<CategoryCountResponse>> Handle(
        ReportGetBookingsByStatusQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetBookingsByStatusAsync(request.From, request.To, cancellationToken);
    }
}
