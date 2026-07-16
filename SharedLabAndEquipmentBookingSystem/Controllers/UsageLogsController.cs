using Application.DTOs.UsageLogs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsageLogsController : ControllerBase
    {
        private readonly IUsageLogService _usageLogService;

        public UsageLogsController(IUsageLogService usageLogService)
        {
            _usageLogService = usageLogService;
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _usageLogService.GetByIdAsync(
                id,
                cancellationToken);

            return result is null
                ? NotFound($"Không tìm thấy UsageLog có ID {id}.")
                : Ok(result);
        }

        [HttpGet("booking-item/{bookingItemId:int}")]
        public async Task<IActionResult> GetByBookingItemId(
            int bookingItemId,
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.GetByBookingItemIdAsync(
                bookingItemId,
                cancellationToken));
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<IActionResult> GetByBookingId(
            int bookingId,
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.GetByBookingIdAsync(
                bookingId,
                cancellationToken));
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet("incidents")]
        public async Task<IActionResult> GetIncidentLogs(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.GetIncidentLogsAsync(
                from,
                to,
                cancellationToken));
        }

        [HttpPost("check-in")]
        [ProducesResponseType(
            typeof(UsageLogResponse),
            StatusCodes.Status201Created)]
        public async Task<IActionResult> CheckIn(
            [FromBody] CheckInUsageRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _usageLogService.CheckInAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.LogId },
                result);
        }

        [HttpPost("{id:int}/check-out")]
        public async Task<IActionResult> CheckOut(
            int id,
            [FromBody] CheckOutUsageRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.CheckOutAsync(
                id,
                request,
                cancellationToken));
        }

        [HttpPost("{id:int}/incident")]
        public async Task<IActionResult> ReportIncident(
            int id,
            [FromBody] ReportUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _usageLogService.ReportIncidentAsync(
                id,
                request,
                cancellationToken));
        }
    }
}
