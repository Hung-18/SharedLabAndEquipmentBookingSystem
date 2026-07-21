using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetMostUsedLabRooms;

public sealed record ReportGetMostUsedLabRoomsQuery(
    DateTime From,
    DateTime To,
    int Top) : IRequest<List<MostUsedResourceResponse>>;

public sealed class ReportGetMostUsedLabRoomsQueryHandler : IRequestHandler<ReportGetMostUsedLabRoomsQuery, List<MostUsedResourceResponse>>
{
    private readonly IReportService _service;

    public ReportGetMostUsedLabRoomsQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<MostUsedResourceResponse>> Handle(
        ReportGetMostUsedLabRoomsQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetMostUsedLabRoomsAsync(request.From, request.To, request.Top, cancellationToken);
    }
}
