using Application.DTOs.Notifications;
using Application.Features.Notifications.Commands.MarkAllAsRead;
using Application.Features.Notifications.Commands.MarkAsRead;
using Application.Features.Notifications.Commands.Send;
using Application.Features.Notifications.Queries.CountUnread;
using Application.Features.Notifications.Queries.GetByUserId;
using Application.Features.Notifications.Queries.GetUnreadByUserId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;
        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new NotificationGetByUserIdQuery(userId, pageNumber, pageSize), cancellationToken));
        }

        [HttpGet("user/{userId:int}/unread")]
        public async Task<IActionResult> GetUnreadByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new NotificationGetUnreadByUserIdQuery(userId), cancellationToken));
        }

        [HttpGet("user/{userId:int}/unread-count")]
        public async Task<IActionResult> CountUnread(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new NotificationCountUnreadQuery(userId), cancellationToken));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("send")]
        public async Task<IActionResult> Send(
            [FromBody] SendNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new NotificationSendCommand(request), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new NotificationMarkAsReadCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("user/{userId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            int userId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new NotificationMarkAllAsReadCommand(userId), cancellationToken);
            return NoContent();
        }
    }
}
