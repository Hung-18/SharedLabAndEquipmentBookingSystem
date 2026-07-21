using Application.DTOs.Violations;
using Application.Features.Violations.Commands.Cancel;
using Application.Features.Violations.Commands.Create;
using Application.Features.Violations.Commands.Resolve;
using Application.Features.Violations.Queries.GetActiveByUserId;
using Application.Features.Violations.Queries.GetAll;
using Application.Features.Violations.Queries.GetByBookingId;
using Application.Features.Violations.Queries.GetById;
using Application.Features.Violations.Queries.GetByUserId;
using Application.Features.Violations.Queries.GetUserSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationsController : ControllerBase
    {
        private readonly ISender _sender;
        public ViolationsController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ViolationGetAllQuery(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ViolationGetByIdQuery(id), cancellationToken);

            return result is null
                ? NotFound($"Không tìm thấy vi phạm có ID {id}.")
                : Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ViolationGetByUserIdQuery(userId), cancellationToken));
        }

        [HttpGet("user/{userId:int}/active")]
        public async Task<IActionResult> GetActiveByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ViolationGetActiveByUserIdQuery(userId), cancellationToken));
        }

        [HttpGet("user/{userId:int}/summary")]
        public async Task<IActionResult> GetUserSummary(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ViolationGetUserSummaryQuery(userId), cancellationToken));
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<IActionResult> GetByBookingId(
            int bookingId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ViolationGetByBookingIdQuery(bookingId), cancellationToken));
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateViolationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ViolationCreateCommand(request), cancellationToken);

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
            await _sender.Send(new ViolationResolveCommand(id), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new ViolationCancelCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
