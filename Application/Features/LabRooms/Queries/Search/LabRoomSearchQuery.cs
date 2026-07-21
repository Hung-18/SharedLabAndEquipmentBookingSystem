using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Queries.Search;

public sealed record LabRoomSearchQuery(
    LabRoomSearchRequest Request) : IRequest<PagedLabRoomResponse>;

public sealed class LabRoomSearchQueryHandler : IRequestHandler<LabRoomSearchQuery, PagedLabRoomResponse>
{
    private readonly ILabRoomService _service;

    public LabRoomSearchQueryHandler(ILabRoomService service)
    {
        _service = service;
    }

    public Task<PagedLabRoomResponse> Handle(
        LabRoomSearchQuery request,
        CancellationToken cancellationToken)
    {
        return _service.SearchAsync(request.Request, cancellationToken);
    }
}
