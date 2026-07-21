using Application.DTOs.LabRooms;
using Application.Interfaces;
using MediatR;

namespace Application.Features.LabRooms.Commands.Create;

public sealed record LabRoomCreateCommand(
    CreateLabRoomRequest Request) : IRequest<LabRoomDetailResponse>;

public sealed class LabRoomCreateCommandHandler : IRequestHandler<LabRoomCreateCommand, LabRoomDetailResponse>
{
    private readonly ILabRoomService _service;

    public LabRoomCreateCommandHandler(ILabRoomService service)
    {
        _service = service;
    }

    public Task<LabRoomDetailResponse> Handle(
        LabRoomCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
