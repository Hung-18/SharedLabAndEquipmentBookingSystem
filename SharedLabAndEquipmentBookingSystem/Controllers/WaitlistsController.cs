using Application.DTOs.Waitlists;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WaitlistsController : ControllerBase
    {
        private readonly IWaitlistService _waitlistService;

        public WaitlistsController(
            IWaitlistService waitlistService)
        {
            _waitlistService = waitlistService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.GetAllAsync(
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.GetByIdAsync(
                id,
                cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy hàng đợi có ID {id}.");
            }

            return Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.GetByUserIdAsync(
                userId,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue(
            [FromQuery] int? labId,
            [FromQuery] int? equipmentId,
            [FromQuery] DateTime requestedStart,
            [FromQuery] DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.GetQueueAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(WaitlistResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.WaitlistId },
                result);
        }

        [HttpPost("notify-next")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> NotifyNext(
            [FromBody] NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.NotifyNextAsync(
                request,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{id:int}/booked")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MarkBooked(
            int id,
            [FromBody] WaitlistActionRequest request,
            CancellationToken cancellationToken)
        {
            await _waitlistService.MarkBookedAsync(
                id,
                request.UserId,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody] WaitlistActionRequest request,
            CancellationToken cancellationToken)
        {
            await _waitlistService.CancelAsync(
                id,
                request.UserId,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:int}/expire")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Expire(
            int id,
            [FromBody] WaitlistActionRequest request,
            CancellationToken cancellationToken)
        {
            await _waitlistService.ExpireAsync(
                id,
                request.UserId,
                cancellationToken);

            return NoContent();
        }
    }

}
