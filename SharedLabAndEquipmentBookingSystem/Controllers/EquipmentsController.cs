using Application.DTOs.Equipments;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentsController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;
        public EquipmentsController(IEquipmentService equipmentService) => _equipmentService = equipmentService;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _equipmentService.GetAllAsync(cancellationToken));

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] EquipmentSearchRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _equipmentService.SearchAsync(request, cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _equipmentService.GetByIdAsync(id, cancellationToken);
            return result is null ? NotFound($"Không tìm thấy thiết bị có ID {id}.") : Ok(result);
        }

        [HttpGet("lab/{labId:int}")]
        public async Task<IActionResult> GetByLabId(int labId, CancellationToken cancellationToken) =>
            Ok(await _equipmentService.GetByLabIdAsync(labId, cancellationToken));

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _equipmentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            await _equipmentService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _equipmentService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
