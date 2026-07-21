using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Queries.GetAll;

public sealed record LabRoomGetAllQuery : IRequest<List<LabRoomResponse>>;

public sealed class LabRoomGetAllQueryHandler : IRequestHandler<LabRoomGetAllQuery, List<LabRoomResponse>>
{
    private readonly ILabRoomService _service;

    public LabRoomGetAllQueryHandler(ILabRoomService service)
    {
        _service = service;
    }

    public Task<List<LabRoomResponse>> Handle(
        LabRoomGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
