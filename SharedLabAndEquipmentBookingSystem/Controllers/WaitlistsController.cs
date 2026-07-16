using Application.DTOs.Waitlists;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WaitlistsController : ControllerBase
    {
        private readonly IWaitlistService _waitlistService;

        public WaitlistsController(IWaitlistService waitlistService)
        {
            _waitlistService = waitlistService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _waitlistService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.GetByIdAsync(id, cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy hàng đợi có ID {id}.")
                : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _waitlistService.GetByUserIdAsync(userId, cancellationToken));
        }

        [HttpGet("queue")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> GetQueue(
            [FromQuery] int? labId,
            [FromQuery] int? equipmentId,
            [FromQuery] DateTime requestedStart,
            [FromQuery] DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            return Ok(await _waitlistService.GetQueueAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _waitlistService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.WaitlistId }, result);
        }

        [HttpPost("notify-next")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> NotifyNext(
            [FromBody] NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _waitlistService.NotifyNextAsync(request, cancellationToken));
        }

        [HttpPost("{id:int}/booked")]
        public async Task<IActionResult> MarkBooked(
            int id,
            CancellationToken cancellationToken)
        {
            await _waitlistService.MarkBookedAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _waitlistService.CancelAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/expire")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> Expire(
            int id,
            CancellationToken cancellationToken)
        {
            await _waitlistService.ExpireAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
