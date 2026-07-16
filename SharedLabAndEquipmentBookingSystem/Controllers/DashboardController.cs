using Application.DTOs.Reports;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,LabManager")]
    public class DashboardController : ControllerBase
    {
        private readonly IReportService _service;

        public DashboardController(IReportService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            DateTime resolvedTo = to ?? DateTime.UtcNow;
            DateTime resolvedFrom = from ?? resolvedTo.AddDays(-30);

            return Ok(await _service.GetDashboardAsync(
                resolvedFrom,
                resolvedTo,
                cancellationToken));
        }
    }
}
