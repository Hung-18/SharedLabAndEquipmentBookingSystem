using Application.DTOs.Reports;
using Application.Features.Reports.Queries.GetBookingsByDepartment;
using Application.Features.Reports.Queries.GetBookingsByPurpose;
using Application.Features.Reports.Queries.GetBookingsByStatus;
using Application.Features.Reports.Queries.GetDepartmentUtilization;
using Application.Features.Reports.Queries.GetEquipmentUtilization;
using Application.Features.Reports.Queries.GetLabUtilization;
using Application.Features.Reports.Queries.GetMaintenanceCostsByEquipment;
using Application.Features.Reports.Queries.GetMaintenanceCostsByLab;
using Application.Features.Reports.Queries.GetMaintenanceHistory;
using Application.Features.Reports.Queries.GetMostUsedEquipments;
using Application.Features.Reports.Queries.GetMostUsedLabRooms;
using Application.Features.Reports.Queries.GetNoShowRate;
using Application.Features.Reports.Queries.GetPenaltyUsers;
using Application.Features.Reports.Queries.GetUsageTrend;
using Application.Features.Reports.Queries.GetViolations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,LabManager")]
    public class ReportsController : ControllerBase
    {
        private readonly ISender _sender;
        public ReportsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("lab-utilization")]
        public async Task<IActionResult> GetLabUtilization(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetLabUtilizationQuery(from, to), cancellationToken));
        }

        [HttpGet("equipment-utilization")]
        public async Task<IActionResult> GetEquipmentUtilization(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetEquipmentUtilizationQuery(from, to), cancellationToken));
        }

        [HttpGet("bookings/by-department")]
        public async Task<IActionResult> GetBookingsByDepartment(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetBookingsByDepartmentQuery(from, to), cancellationToken));
        }

        [HttpGet("department-utilization")]
        [ProducesResponseType(
            typeof(List<DepartmentUtilizationResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartmentUtilization(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetDepartmentUtilizationQuery(from, to), cancellationToken));
        }

        [HttpGet("bookings/by-purpose")]
        public async Task<IActionResult> GetBookingsByPurpose(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetBookingsByPurposeQuery(from, to), cancellationToken));
        }

        [HttpGet("bookings/by-status")]
        public async Task<IActionResult> GetBookingsByStatus(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetBookingsByStatusQuery(from, to), cancellationToken));
        }

        [HttpGet("maintenance-costs/by-lab")]
        public async Task<IActionResult> GetMaintenanceCostsByLab(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetMaintenanceCostsByLabQuery(from, to), cancellationToken));
        }

        [HttpGet("maintenance-costs/by-equipment")]
        public async Task<IActionResult> GetMaintenanceCostsByEquipment(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetMaintenanceCostsByEquipmentQuery(from, to), cancellationToken));
        }

        [HttpGet("maintenance-history")]
        [ProducesResponseType(
            typeof(PagedMaintenanceHistoryResponse),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMaintenanceHistory(
            [FromQuery] MaintenanceHistoryQueryRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetMaintenanceHistoryQuery(request), cancellationToken));
        }

        [HttpGet("most-used/labs")]
        public async Task<IActionResult> GetMostUsedLabRooms(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new ReportGetMostUsedLabRoomsQuery(from, to, top), cancellationToken));
        }

        [HttpGet("most-used/equipments")]
        public async Task<IActionResult> GetMostUsedEquipments(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new ReportGetMostUsedEquipmentsQuery(from, to, top), cancellationToken));
        }

        [HttpGet("violations")]
        [ProducesResponseType(typeof(ViolationSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetViolations(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetViolationsQuery(from, to), cancellationToken));
        }

        [HttpGet("penalty-users")]
        public async Task<IActionResult> GetPenaltyUsers(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int top = 10,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new ReportGetPenaltyUsersQuery(from, to, top), cancellationToken));
        }

        [HttpGet("no-show-rate")]
        public async Task<IActionResult> GetNoShowRate(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ReportGetNoShowRateQuery(from, to), cancellationToken));
        }

        [HttpGet("usage-trend")]
        public async Task<IActionResult> GetUsageTrend(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string groupBy = "day",
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new ReportGetUsageTrendQuery(from, to, groupBy), cancellationToken));
        }
    }
}
