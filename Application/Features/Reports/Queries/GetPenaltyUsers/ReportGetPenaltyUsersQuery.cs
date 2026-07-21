using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetPenaltyUsers;

public sealed record ReportGetPenaltyUsersQuery(
    DateTime From,
    DateTime To,
    int Top) : IRequest<List<PenaltyUserReportResponse>>;

public sealed class ReportGetPenaltyUsersQueryHandler : IRequestHandler<ReportGetPenaltyUsersQuery, List<PenaltyUserReportResponse>>
{
    private readonly IReportService _service;

    public ReportGetPenaltyUsersQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<PenaltyUserReportResponse>> Handle(
        ReportGetPenaltyUsersQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetPenaltyUsersAsync(request.From, request.To, request.Top, cancellationToken);
    }
}
