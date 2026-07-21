using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Queries.GetById;

public sealed record LabRoomGetByIdQuery(
    int Id) : IRequest<LabRoomDetailResponse?>;

public sealed class LabRoomGetByIdQueryHandler : IRequestHandler<LabRoomGetByIdQuery, LabRoomDetailResponse?>
{
    private readonly ILabRoomService _service;

    public LabRoomGetByIdQueryHandler(ILabRoomService service)
    {
        _service = service;
    }

    public Task<LabRoomDetailResponse?> Handle(
        LabRoomGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
