using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetBookingsByDepartment;

public sealed record ReportGetBookingsByDepartmentQuery(
    DateTime From,
    DateTime To) : IRequest<List<CategoryCountResponse>>;

public sealed class ReportGetBookingsByDepartmentQueryHandler : IRequestHandler<ReportGetBookingsByDepartmentQuery, List<CategoryCountResponse>>
{
    private readonly IReportService _service;

    public ReportGetBookingsByDepartmentQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<CategoryCountResponse>> Handle(
        ReportGetBookingsByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetBookingsByDepartmentAsync(request.From, request.To, cancellationToken);
    }
}
