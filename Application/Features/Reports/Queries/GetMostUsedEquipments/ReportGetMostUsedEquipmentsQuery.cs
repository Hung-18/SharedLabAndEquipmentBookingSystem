using Application.DTOs.Reports;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Reports.Queries.GetMostUsedEquipments;

public sealed record ReportGetMostUsedEquipmentsQuery(
    DateTime From,
    DateTime To,
    int Top) : IRequest<List<MostUsedResourceResponse>>;

public sealed class ReportGetMostUsedEquipmentsQueryHandler : IRequestHandler<ReportGetMostUsedEquipmentsQuery, List<MostUsedResourceResponse>>
{
    private readonly IReportService _service;

    public ReportGetMostUsedEquipmentsQueryHandler(IReportService service)
    {
        _service = service;
    }

    public Task<List<MostUsedResourceResponse>> Handle(
        ReportGetMostUsedEquipmentsQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetMostUsedEquipmentsAsync(request.From, request.To, request.Top, cancellationToken);
    }
}
