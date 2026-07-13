using Application.DTOs.Equipments;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentsController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentsController(
            IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        // GET: api/Equipments
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var equipments =
                await _equipmentService.GetAllAsync(
                    cancellationToken);

            return Ok(equipments);
        }

        // GET: api/Equipments/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(EquipmentDetailResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var equipment =
                await _equipmentService.GetByIdAsync(
                    id,
                    cancellationToken);

            if (equipment is null)
            {
                return NotFound(
                    $"Không tìm thấy thiết bị có ID {id}.");
            }

            return Ok(equipment);
        }

        // GET: api/Equipments/lab/1
        [HttpGet("lab/{labId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByLabId(
            int labId,
            CancellationToken cancellationToken)
        {
            var equipments =
                await _equipmentService.GetByLabIdAsync(
                    labId,
                    cancellationToken);

            return Ok(equipments);
        }

        // POST: api/Equipments
        [HttpPost]
        [ProducesResponseType(
            typeof(EquipmentDetailResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(
            [FromBody] CreateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            var createdEquipment =
                await _equipmentService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEquipment.EquipmentId },
                createdEquipment);
        }

        // PUT: api/Equipments/1
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            await _equipmentService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }

        // DELETE: api/Equipments/1
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            await _equipmentService.DeleteAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}
