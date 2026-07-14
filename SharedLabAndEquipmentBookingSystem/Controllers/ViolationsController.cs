using Application.DTOs.Violations;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationsController : ControllerBase
    {
        private readonly IViolationService _violationService;

        public ViolationsController(
            IViolationService violationService)
        {
            _violationService = violationService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetAllAsync(
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(ViolationResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetByIdAsync(
                    id,
                    cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy vi phạm có ID {id}.");
            }

            return Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("user/{userId:int}/active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("user/{userId:int}/summary")]
        [ProducesResponseType(
            typeof(UserViolationSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserSummary(
            int userId,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetUserSummaryAsync(
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("booking/{bookingId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBookingId(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.GetByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(ViolationResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateViolationRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _violationService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ViolationId },
                result);
        }

        [HttpPost("{id:int}/resolve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Resolve(
            int id,
            [FromBody] ViolationActionRequest request,
            CancellationToken cancellationToken)
        {
            await _violationService.ResolveAsync(
                id,
                request,
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
            [FromBody] ViolationActionRequest request,
            CancellationToken cancellationToken)
        {
            await _violationService.CancelAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }
    }

}
