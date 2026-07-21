using Application.DTOs.Equipments;
using Application.Features.Equipments.Commands.Create;
using Application.Features.Equipments.Commands.Delete;
using Application.Features.Equipments.Commands.Update;
using Application.Features.Equipments.Queries.GetAll;
using Application.Features.Equipments.Queries.GetById;
using Application.Features.Equipments.Queries.GetByLabId;
using Application.Features.Equipments.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentsController : ControllerBase
    {
        private readonly ISender _sender;
        public EquipmentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new EquipmentGetAllQuery(), cancellationToken));

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] EquipmentSearchRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new EquipmentSearchQuery(request), cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new EquipmentGetByIdQuery(id), cancellationToken);
            return result is null ? NotFound($"Không tìm thấy thiết bị có ID {id}.") : Ok(result);
        }

        [HttpGet("lab/{labId:int}")]
        public async Task<IActionResult> GetByLabId(int labId, CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new EquipmentGetByLabIdQuery(labId), cancellationToken));

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new EquipmentCreateCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new EquipmentUpdateCommand(id, request), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _sender.Send(new EquipmentDeleteCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
