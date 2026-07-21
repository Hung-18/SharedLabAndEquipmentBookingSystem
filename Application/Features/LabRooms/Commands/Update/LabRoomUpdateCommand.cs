using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Commands.Update;

public sealed record LabRoomUpdateCommand(
    int Id,
    UpdateLabRoomRequest Request) : IRequest<bool>;

public sealed class LabRoomUpdateCommandHandler : IRequestHandler<LabRoomUpdateCommand, bool>
{
    private readonly ILabRoomService _service;

    public LabRoomUpdateCommandHandler(ILabRoomService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        LabRoomUpdateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
