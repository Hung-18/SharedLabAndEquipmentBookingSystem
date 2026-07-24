using Application.DTOs;
using Application.DTOs.Booking;
using Application.Features.Bookings.Commands.Approve;
using Application.Features.Bookings.Commands.Cancel;
using Application.Features.Bookings.Commands.Complete;
using Application.Features.Bookings.Commands.Create;
using Application.Features.Bookings.Commands.MarkNoShow;
using Application.Features.Bookings.Commands.Reject;
using Application.Features.Bookings.Commands.Update;
using Application.Features.Bookings.Queries.GetAll;
using Application.Features.Bookings.Queries.GetById;
using Application.Features.Bookings.Queries.GetByUserId;
using Application.Features.Bookings.Queries.GetCalendar;
using Application.Features.Bookings.Queries.GetPending;
using Application.Features.Bookings.Queries.SuggestAlternativeSlots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly ISender _sender;
        public BookingsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,LabManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new BookingGetAllQuery(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BookingDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new BookingGetByIdQuery(id), cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy booking có ID {id}.")
                : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new BookingGetByUserIdQuery(userId), cancellationToken));
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new BookingGetPendingQuery(), cancellationToken));
        }

        [HttpGet("calendar")]
        [ProducesResponseType(typeof(List<CalendarEventResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCalendar(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? labId,
            [FromQuery] int? equipmentId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new BookingGetCalendarQuery(from, to, labId, equipmentId), cancellationToken));
        }

        [HttpPost("suggested-slots")]
        [ProducesResponseType(typeof(List<SuggestedSlotResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SuggestAlternativeSlots(
            [FromBody] AlternativeSlotRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new BookingSuggestAlternativeSlotsQuery(request), cancellationToken));
        }

        [HttpPost]
        [Authorize(Roles ="Requester")]
        [ProducesResponseType(typeof(BookingDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new BookingCreateCommand(request), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.BookingId },
                result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBookingRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingUpdateCommand(id, request), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "LabManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Approve(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingApproveCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "LabManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectBookingRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingRejectCommand(id, request), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingCancelCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/complete")]
        [Authorize(Roles = "Admin,LabManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Complete(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingCompleteCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/no-show")]
        [Authorize(Roles = "Admin,LabManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkNoShow(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new BookingMarkNoShowCommand(id), cancellationToken);
            return NoContent();
        }

        
    }
}
