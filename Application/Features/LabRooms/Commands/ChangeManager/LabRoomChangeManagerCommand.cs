using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Commands.ChangeManager;

public sealed record LabRoomChangeManagerCommand(
    int Id,
    ChangeLabRoomManagerRequest Request) : IRequest<bool>;

public sealed class LabRoomChangeManagerCommandHandler : IRequestHandler<LabRoomChangeManagerCommand, bool>
{
    private readonly ILabRoomService _service;

    public LabRoomChangeManagerCommandHandler(ILabRoomService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        LabRoomChangeManagerCommand request,
        CancellationToken cancellationToken)
    {
        await _service.ChangeManagerAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
