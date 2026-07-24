using Application.DTOs.Maintenances;
using Application.Features.Maintenances.Commands.Cancel;
using Application.Features.Maintenances.Commands.CancelSeries;
using Application.Features.Maintenances.Commands.Complete;
using Application.Features.Maintenances.Commands.Create;
using Application.Features.Maintenances.Commands.Start;
using Application.Features.Maintenances.Commands.Update;
using Application.Features.Maintenances.Queries.GetAll;
using Application.Features.Maintenances.Queries.GetByEquipmentId;
using Application.Features.Maintenances.Queries.GetById;
using Application.Features.Maintenances.Queries.GetByLabId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenancesController : ControllerBase
    {
        private readonly ISender _sender;
        public MaintenancesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MaintenanceGetAllQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MaintenanceGetByIdQuery(id), cancellationToken);

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
            var result = await _sender.Send(new MaintenanceGetByLabIdQuery(labId), cancellationToken);

            return Ok(result);
        }

        [HttpGet("equipment/{equipmentId:int}")]
        public async Task<IActionResult> GetByEquipmentId(
            int equipmentId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MaintenanceGetByEquipmentIdQuery(equipmentId), cancellationToken);

            return Ok(result);
        }

        [Authorize(Roles = "LabManager")]
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
            var result = await _sender.Send(new MaintenanceCreateCommand(request), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.MaintenanceId },
                result);
        }

        [Authorize(Roles = "LabManager")]
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
            await _sender.Send(new MaintenanceUpdateCommand(id, request), cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "LabManager")]
        [HttpPost("{id:int}/start")]
        public async Task<IActionResult> Start(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MaintenanceStartCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "LabManager")]
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MaintenanceCompleteCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "LabManager")]
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MaintenanceCancelCommand(id), cancellationToken);

            return NoContent();
        }


        [Authorize(Roles = "LabManager")]
        [HttpPost("{id:int}/cancel-series")]
        public async Task<IActionResult> CancelSeries(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MaintenanceCancelSeriesCommand(id), cancellationToken);

            return NoContent();
        }
    }
}
