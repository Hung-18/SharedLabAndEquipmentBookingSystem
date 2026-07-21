using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetBookingsByPurpose;

public sealed record ReportGetBookingsByPurposeQuery(
    DateTime From,
    DateTime To) : IRequest<List<CategoryCountResponse>>;

public sealed class ReportGetBookingsByPurposeQueryHandler : IRequestHandler<ReportGetBookingsByPurposeQuery, List<CategoryCountResponse>>
{
    private readonly IReportService _service;

    public ReportGetBookingsByPurposeQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<CategoryCountResponse>> Handle(
        ReportGetBookingsByPurposeQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetBookingsByPurposeAsync(request.From, request.To, cancellationToken);
    }
}
