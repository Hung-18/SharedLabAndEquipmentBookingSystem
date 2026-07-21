using Application.DTOs.LabRooms;
using Application.Features.LabRooms.Commands.ChangeManager;
using Application.Features.LabRooms.Commands.Create;
using Application.Features.LabRooms.Commands.Delete;
using Application.Features.LabRooms.Commands.Update;
using Application.Features.LabRooms.Queries.GetAll;
using Application.Features.LabRooms.Queries.GetById;
using Application.Features.LabRooms.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LabRoomsController : ControllerBase
    {
        private readonly ISender _sender;
        public LabRoomsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new LabRoomGetAllQuery(), cancellationToken));

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] LabRoomSearchRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new LabRoomSearchQuery(request), cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var room = await _sender.Send(new LabRoomGetByIdQuery(id), cancellationToken);
            return room is null ? NotFound($"Không tìm thấy phòng lab có ID {id}.") : Ok(room);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LabRoomCreateCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.LabId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new LabRoomUpdateCommand(id, request), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/manager")]
        public async Task<IActionResult> ChangeManager(
            int id,
            [FromBody] ChangeLabRoomManagerRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new LabRoomChangeManagerCommand(id, request), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _sender.Send(new LabRoomDeleteCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
