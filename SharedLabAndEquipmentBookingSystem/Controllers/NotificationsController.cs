using Application.DTOs.Notifications;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(
            int userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _notificationService.GetByUserIdAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken));
        }

        [HttpGet("user/{userId:int}/unread")]
        public async Task<IActionResult> GetUnreadByUserId(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _notificationService.GetUnreadByUserIdAsync(
                userId,
                cancellationToken));
        }

        [HttpGet("user/{userId:int}/unread-count")]
        public async Task<IActionResult> CountUnread(
            int userId,
            CancellationToken cancellationToken)
        {
            return Ok(await _notificationService.CountUnreadAsync(
                userId,
                cancellationToken));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("send")]
        public async Task<IActionResult> Send(
            [FromBody] SendNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.SendAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(
            int id,
            CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("user/{userId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            int userId,
            CancellationToken cancellationToken)
        {
            await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            return NoContent();
        }
    }
}
