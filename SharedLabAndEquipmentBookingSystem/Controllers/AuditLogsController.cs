using Application.DTOs.AuditLogs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(
            IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        // GET:
        // api/AuditLogs?actorUserId=1&pageNumber=1&pageSize=20
        [HttpGet]
        [ProducesResponseType(
            typeof(PagedAuditLogResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Search(
            [FromQuery] AuditLogQueryRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _auditLogService.SearchAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }

        // GET: api/AuditLogs/1?actorUserId=1
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(AuditLogResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            [FromQuery] int? actorUserId,
            CancellationToken cancellationToken)
        {
            var result =
                await _auditLogService.GetByIdAsync(
                    id,
                    actorUserId,
                    cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy AuditLog có ID {id}.");
            }

            return Ok(result);
        }
    }

}
