using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Commands.Delete;

public sealed record LabRoomDeleteCommand(
    int Id) : IRequest<bool>;

public sealed class LabRoomDeleteCommandHandler : IRequestHandler<LabRoomDeleteCommand, bool>
{
    private readonly ILabRoomService _service;

    public LabRoomDeleteCommandHandler(ILabRoomService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        LabRoomDeleteCommand request,
        CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
