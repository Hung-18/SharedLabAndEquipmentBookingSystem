using Application.DTOs.Maintenances;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenancesController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenancesController(
            IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _maintenanceService.GetAllAsync(
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _maintenanceService.GetByIdAsync(
                id,
                cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy lịch bảo trì có ID {id}.");
            }

            return Ok(result);
        }

        [HttpGet("lab/{labId:int}")]
        public async Task<IActionResult> GetByLabId(
            int labId,
            CancellationToken cancellationToken)
        {
            var result = await _maintenanceService.GetByLabIdAsync(
                labId,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("equipment/{equipmentId:int}")]
        public async Task<IActionResult> GetByEquipmentId(
            int equipmentId,
            CancellationToken cancellationToken)
        {
            var result = await _maintenanceService.GetByEquipmentIdAsync(
                equipmentId,
                cancellationToken);

            return Ok(result);
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost]
        [ProducesResponseType(
            typeof(MaintenanceDetailResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _maintenanceService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.MaintenanceId },
                result);
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            await _maintenanceService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/start")]
        public async Task<IActionResult> Start(
            int id,
            CancellationToken cancellationToken)
        {
            await _maintenanceService.StartAsync(
                id,
                cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(
            int id,
            CancellationToken cancellationToken)
        {
            await _maintenanceService.CompleteAsync(
                id,
                cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _maintenanceService.CancelAsync(
                id,
                cancellationToken);

            return NoContent();
        }


        [Authorize(Roles = "Admin,LabManager")]
        [HttpPost("{id:int}/cancel-series")]
        public async Task<IActionResult> CancelSeries(
            int id,
            CancellationToken cancellationToken)
        {
            await _maintenanceService.CancelSeriesAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}
