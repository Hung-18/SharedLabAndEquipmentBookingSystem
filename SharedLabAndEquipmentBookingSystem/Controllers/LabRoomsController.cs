using Application.DTOs.LabRooms;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LabRoomsController : ControllerBase
    {
        private readonly ILabRoomService _labRoomService;
        public LabRoomsController(ILabRoomService labRoomService) => _labRoomService = labRoomService;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _labRoomService.GetAllAsync(cancellationToken));

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] LabRoomSearchRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _labRoomService.SearchAsync(request, cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var room = await _labRoomService.GetByIdAsync(id, cancellationToken);
            return room is null ? NotFound($"Không tìm thấy phòng lab có ID {id}.") : Ok(room);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _labRoomService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.LabId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            await _labRoomService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/manager")]
        public async Task<IActionResult> ChangeManager(
            int id,
            [FromBody] ChangeLabRoomManagerRequest request,
            CancellationToken cancellationToken)
        {
            await _labRoomService.ChangeManagerAsync(id, request, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _labRoomService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
