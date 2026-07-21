using Application.DTOs.Waitlists;
using Application.Features.Waitlists.Commands.Cancel;
using Application.Features.Waitlists.Commands.Create;
using Application.Features.Waitlists.Commands.Expire;
using Application.Features.Waitlists.Commands.MarkBooked;
using Application.Features.Waitlists.Commands.NotifyNext;
using Application.Features.Waitlists.Queries.GetAll;
using Application.Features.Waitlists.Queries.GetById;
using Application.Features.Waitlists.Queries.GetByUserId;
using Application.Features.Waitlists.Queries.GetQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WaitlistsController : ControllerBase
    {
        private readonly ISender _sender;
        public WaitlistsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new WaitlistGetAllQuery(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new WaitlistGetByIdQuery(id), cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy hàng đợi có ID {id}.")
                : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new WaitlistGetByUserIdQuery(userId), cancellationToken));
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
            return Ok(await _sender.Send(new WaitlistGetQueueQuery(labId, equipmentId, requestedStart, requestedEnd), cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new WaitlistCreateCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.WaitlistId }, result);
        }

        [HttpPost("notify-next")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> NotifyNext(
            [FromBody] NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new WaitlistNotifyNextCommand(request), cancellationToken));
        }

        [HttpPost("{id:int}/booked")]
        public async Task<IActionResult> MarkBooked(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new WaitlistMarkBookedCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new WaitlistCancelCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/expire")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> Expire(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new WaitlistExpireCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
