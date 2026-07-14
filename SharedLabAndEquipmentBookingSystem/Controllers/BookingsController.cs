using Application.DTOs.Booking;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _bookingService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BookingDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _bookingService.GetByIdAsync(id, cancellationToken);
            return result is null ? NotFound($"Không tìm thấy booking có ID {id}.") : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId, CancellationToken cancellationToken)
        {
            return Ok(await _bookingService.GetByUserIdAsync(userId, cancellationToken));
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            return Ok(await _bookingService.GetPendingAsync(cancellationToken));
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? labId,
            [FromQuery] int? equipmentId,
            CancellationToken cancellationToken)
        {
            return Ok(await _bookingService.GetCalendarAsync(
                from,
                to,
                labId,
                equipmentId,
                cancellationToken));
        }

        [HttpPost]
        [ProducesResponseType(typeof(BookingDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _bookingService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.BookingId },
                result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBookingRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.ApproveAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectBookingRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.RejectAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody] BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.CancelAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Complete(
            int id,
            [FromBody] BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.CompleteAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/no-show")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkNoShow(
            int id,
            [FromBody] BookingActionRequest request,
            CancellationToken cancellationToken)
        {
            await _bookingService.MarkNoShowAsync(id, request, cancellationToken);
            return NoContent();
        }
    }

}
