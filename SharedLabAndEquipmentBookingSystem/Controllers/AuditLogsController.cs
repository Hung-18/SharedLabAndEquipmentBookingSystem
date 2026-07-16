using Application.DTOs.AuditLogs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] AuditLogQueryRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _auditLogService.SearchAsync(request, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _auditLogService.GetByIdAsync(id, cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy AuditLog có ID {id}.")
                : Ok(result);
        }
    }
}
