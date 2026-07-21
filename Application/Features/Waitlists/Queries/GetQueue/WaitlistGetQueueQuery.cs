using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Queries.GetQueue;

public sealed record WaitlistGetQueueQuery(
    int? LabId,
    int? EquipmentId,
    DateTime RequestedStart,
    DateTime RequestedEnd) : IRequest<List<WaitlistResponse>>;

public sealed class WaitlistGetQueueQueryHandler : IRequestHandler<WaitlistGetQueueQuery, List<WaitlistResponse>>
{
    private readonly IWaitlistService _service;

    public WaitlistGetQueueQueryHandler(IWaitlistService service)
    {
        _service = service;
    }

    public Task<List<WaitlistResponse>> Handle(
        WaitlistGetQueueQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetQueueAsync(request.LabId, request.EquipmentId, request.RequestedStart, request.RequestedEnd, cancellationToken);
    }
}
