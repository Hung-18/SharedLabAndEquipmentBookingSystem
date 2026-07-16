using Application.DTOs.Reports;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,LabManager")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        [HttpGet("lab-utilization")]
        public async Task<IActionResult> GetLabUtilization(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetLabUtilizationAsync(from, to, cancellationToken));
        }

        [HttpGet("equipment-utilization")]
        public async Task<IActionResult> GetEquipmentUtilization(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetEquipmentUtilizationAsync(from, to, cancellationToken));
        }

        [HttpGet("bookings/by-department")]
        public async Task<IActionResult> GetBookingsByDepartment(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetBookingsByDepartmentAsync(from, to, cancellationToken));
        }

        [HttpGet("bookings/by-purpose")]
        public async Task<IActionResult> GetBookingsByPurpose(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetBookingsByPurposeAsync(from, to, cancellationToken));
        }

        [HttpGet("bookings/by-status")]
        public async Task<IActionResult> GetBookingsByStatus(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetBookingsByStatusAsync(from, to, cancellationToken));
        }

        [HttpGet("maintenance-costs/by-lab")]
        public async Task<IActionResult> GetMaintenanceCostsByLab(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetMaintenanceCostsByLabAsync(from, to, cancellationToken));
        }

        [HttpGet("maintenance-costs/by-equipment")]
        public async Task<IActionResult> GetMaintenanceCostsByEquipment(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetMaintenanceCostsByEquipmentAsync(from, to, cancellationToken));
        }

        [HttpGet("most-used/labs")]
        public async Task<IActionResult> GetMostUsedLabRooms(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _service.GetMostUsedLabRoomsAsync(from, to, top, cancellationToken));
        }

        [HttpGet("most-used/equipments")]
        public async Task<IActionResult> GetMostUsedEquipments(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _service.GetMostUsedEquipmentsAsync(from, to, top, cancellationToken));
        }

        [HttpGet("violations")]
        [ProducesResponseType(typeof(ViolationSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetViolations(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetViolationsAsync(from, to, cancellationToken));
        }

        [HttpGet("penalty-users")]
        public async Task<IActionResult> GetPenaltyUsers(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _service.GetPenaltyUsersAsync(from, to, top, cancellationToken));
        }

        [HttpGet("no-show-rate")]
        public async Task<IActionResult> GetNoShowRate(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetNoShowRateAsync(from, to, cancellationToken));
        }

        [HttpGet("usage-trend")]
        public async Task<IActionResult> GetUsageTrend(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string groupBy = "day",
            CancellationToken cancellationToken = default)
        {
            return Ok(await _service.GetUsageTrendAsync(from, to, groupBy, cancellationToken));
        }
    }
}
