using Application.DTOs.PriorityRules;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PriorityRulesController : ControllerBase
    {
        private readonly IPriorityRuleService _priorityRuleService;

        public PriorityRulesController(
            IPriorityRuleService priorityRuleService)
        {
            _priorityRuleService = priorityRuleService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result =
                await _priorityRuleService.GetAllAsync(
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActive(
            CancellationToken cancellationToken)
        {
            var result =
                await _priorityRuleService.GetActiveAsync(
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(PriorityRuleResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result =
                await _priorityRuleService.GetByIdAsync(
                    id,
                    cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy quy tắc ưu tiên có ID {id}.");
            }

            return Ok(result);
        }

        [HttpGet("purpose/{purposeType}")]
        [ProducesResponseType(
            typeof(PriorityRuleResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByPurposeType(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            var result =
                await _priorityRuleService.GetByPurposeTypeAsync(
                    purposeType,
                    cancellationToken);

            if (result is null)
            {
                return NotFound(
                    $"Không tìm thấy quy tắc cho mục đích {purposeType}.");
            }

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(PriorityRuleResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            var result =
                await _priorityRuleService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.PriorityRuleId },
                result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:int}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(
            int id,
            [FromBody] PriorityRuleActionRequest request,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.ActivateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(
            int id,
            [FromBody] PriorityRuleActionRequest request,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.DeactivateAsync(
                id,
                request,
                cancellationToken);

            return NoContent();
        }
    }

}
