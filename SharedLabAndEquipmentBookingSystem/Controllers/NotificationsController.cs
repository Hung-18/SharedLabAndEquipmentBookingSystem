using Application.DTOs.Notifications;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUserId(
            int userId,
            [FromQuery] int actorUserId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result =
                await _notificationService.GetByUserIdAsync(
                    actorUserId,
                    userId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("user/{userId:int}/unread")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUnreadByUserId(
            int userId,
            [FromQuery] int actorUserId,
            CancellationToken cancellationToken)
        {
            var result =
                await _notificationService.GetUnreadByUserIdAsync(
                    actorUserId,
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("user/{userId:int}/unread-count")]
        [ProducesResponseType(
            typeof(UnreadNotificationCountResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CountUnread(
            int userId,
            [FromQuery] int actorUserId,
            CancellationToken cancellationToken)
        {
            var result =
                await _notificationService.CountUnreadAsync(
                    actorUserId,
                    userId,
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost("send")]
        [ProducesResponseType(
            typeof(NotificationResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Send(
            [FromBody] SendNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _notificationService.SendAsync(
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }

        [HttpPost("{id:int}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(
            int id,
            [FromBody] NotificationActionRequest request,
            CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("user/{userId:int}/read-all")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAllAsRead(
            int userId,
            [FromBody] NotificationActionRequest request,
            CancellationToken cancellationToken)
        {
            await _notificationService.MarkAllAsReadAsync(
                userId,
                request,
                cancellationToken);

            return NoContent();
        }
    }

}
