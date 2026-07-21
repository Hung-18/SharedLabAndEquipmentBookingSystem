using Application.DTOs.UsageLogs;
using Application.Features.UsageLogs.Commands.CheckIn;
using Application.Features.UsageLogs.Commands.CheckOut;
using Application.Features.UsageLogs.Commands.ConfirmIncident;
using Application.Features.UsageLogs.Commands.RejectIncident;
using Application.Features.UsageLogs.Commands.ReportIncident;
using Application.Features.UsageLogs.Queries.GetAll;
using Application.Features.UsageLogs.Queries.GetByBookingId;
using Application.Features.UsageLogs.Queries.GetByBookingItemId;
using Application.Features.UsageLogs.Queries.GetById;
using Application.Features.UsageLogs.Queries.GetIncidentLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsageLogsController : ControllerBase
    {
        private readonly ISender _sender;
        public UsageLogsController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogGetAllQuery(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UsageLogGetByIdQuery(id), cancellationToken);

            return result is null
                ? NotFound($"Không tìm thấy UsageLog có ID {id}.")
                : Ok(result);
        }

        [HttpGet("booking-item/{bookingItemId:int}")]
        public async Task<IActionResult> GetByBookingItemId(
            int bookingItemId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogGetByBookingItemIdQuery(bookingItemId), cancellationToken));
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<IActionResult> GetByBookingId(
            int bookingId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogGetByBookingIdQuery(bookingId), cancellationToken));
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet("incidents")]
        public async Task<IActionResult> GetIncidentLogs(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogGetIncidentLogsQuery(from, to), cancellationToken));
        }

        [HttpPost("check-in")]
        [ProducesResponseType(
            typeof(UsageLogResponse),
            StatusCodes.Status201Created)]
        public async Task<IActionResult> CheckIn(
            [FromBody] CheckInUsageRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UsageLogCheckInCommand(request), cancellationToken);

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
            return Ok(await _sender.Send(new UsageLogCheckOutCommand(id, request), cancellationToken));
        }

        [HttpPost("{id:int}/incident")]
        public async Task<IActionResult> ReportIncident(
            int id,
            [FromBody] ReportUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogReportIncidentCommand(id, request), cancellationToken));
        }


        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/incident/confirm")]
        public async Task<IActionResult> ConfirmIncident(
            int id,
            [FromBody] ReviewUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogConfirmIncidentCommand(id, request), cancellationToken));
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/incident/reject")]
        public async Task<IActionResult> RejectIncident(
            int id,
            [FromBody] ReviewUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UsageLogRejectIncidentCommand(id, request), cancellationToken));
        }
    }
}
