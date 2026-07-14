using Application.DTOs.UsageLogs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsageLogsController : ControllerBase
    {
        private readonly IUsageLogService _usageLogService;

        public UsageLogsController(
            IUsageLogService usageLogService)
        {
            _usageLogService = usageLogService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.GetAllAsync(
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
            var result =
                await _usageLogService.GetByIdAsync(
                    id,
                    cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy UsageLog có ID {id}.");
            }

            return Ok(result);
        }

        [HttpGet("booking-item/{bookingItemId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBookingItemId(
            int bookingItemId,
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.GetByBookingItemIdAsync(
                    bookingItemId,
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
                await _usageLogService.GetByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("incidents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIncidentLogs(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.GetIncidentLogsAsync(
                    from,
                    to,
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost("check-in")]
        [ProducesResponseType(
            typeof(UsageLogResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckIn(
            [FromBody] CheckInUsageRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.CheckInAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.LogId },
                result);
        }

        [HttpPost("{id:int}/check-out")]
        [ProducesResponseType(
            typeof(UsageLogResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckOut(
            int id,
            [FromBody] CheckOutUsageRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.CheckOutAsync(
                    id,
                    request,
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost("{id:int}/incident")]
        [ProducesResponseType(
            typeof(UsageLogResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReportIncident(
            int id,
            [FromBody] ReportUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _usageLogService.ReportIncidentAsync(
                    id,
                    request,
                    cancellationToken);

            return Ok(result);
        }
    }

}
