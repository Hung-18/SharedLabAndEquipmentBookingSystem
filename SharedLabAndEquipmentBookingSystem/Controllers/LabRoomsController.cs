using Application.DTOs.LabRooms;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LabRoomsController : ControllerBase
    {
        private readonly ILabRoomService _labRoomService;

        public LabRoomsController(ILabRoomService labRoomService)
        {
            _labRoomService = labRoomService;
        }

        // GET: api/LabRooms
        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var rooms = await _labRoomService.GetAllAsync(
                cancellationToken);

            return Ok(rooms);
        }

        // GET: api/LabRooms/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var room = await _labRoomService.GetByIdAsync(
                id,
                cancellationToken);

            if (room == null)
            {
                return NotFound("Lab room not found");
            }

            return Ok(room);
        }

        // POST: api/LabRooms
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            var createdRoom = await _labRoomService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdRoom.LabId },
                createdRoom);
        }

        // PUT: api/LabRooms/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            await _labRoomService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }
    }
}