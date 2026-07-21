using Application.DTOs.AuditLogs;
using Application.Features.AuditLogs.Queries.GetById;
using Application.Features.AuditLogs.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly ISender _sender;
        public AuditLogsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] AuditLogQueryRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new AuditLogSearchQuery(request), cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new AuditLogGetByIdQuery(id), cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy AuditLog có ID {id}.")
                : Ok(result);
        }
    }
}
