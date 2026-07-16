using Application.DTOs.Violations;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationsController : ControllerBase
    {
        private readonly IViolationService _violationService;

        public ViolationsController(IViolationService violationService)
        {
            _violationService = violationService;
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _violationService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _violationService.GetByIdAsync(
                id,
                cancellationToken);

            return result is null
                ? NotFound($"Không tìm thấy vi phạm có ID {id}.")
                : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _violationService.GetByUserIdAsync(
                userId,
                cancellationToken));
        }

        [HttpGet("user/{userId:int}/active")]
        public async Task<IActionResult> GetActiveByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _violationService.GetActiveByUserIdAsync(
                userId,
                cancellationToken));
        }

        [HttpGet("user/{userId:int}/summary")]
        public async Task<IActionResult> GetUserSummary(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _violationService.GetUserSummaryAsync(
                userId,
                cancellationToken));
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<IActionResult> GetByBookingId(
            int bookingId,
            CancellationToken cancellationToken)
        {
            return Ok(await _violationService.GetByBookingIdAsync(
                bookingId,
                cancellationToken));
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateViolationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _violationService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ViolationId },
                result);
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/resolve")]
        public async Task<IActionResult> Resolve(
            int id,
            CancellationToken cancellationToken)
        {
            await _violationService.ResolveAsync(id, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _violationService.CancelAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
