using Application.DTOs.PriorityRules;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PriorityRulesController : ControllerBase
    {
        private readonly IPriorityRuleService _priorityRuleService;

        public PriorityRulesController(IPriorityRuleService priorityRuleService)
        {
            _priorityRuleService = priorityRuleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _priorityRuleService.GetAllAsync(cancellationToken));

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken) =>
            Ok(await _priorityRuleService.GetActiveAsync(cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _priorityRuleService.GetByIdAsync(id, cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy quy tắc ưu tiên có ID {id}.")
                : Ok(result);
        }

        [HttpGet("purpose/{purposeType}")]
        public async Task<IActionResult> GetByPurposeType(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            var result = await _priorityRuleService.GetByPurposeTypeAsync(
                purposeType,
                cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy quy tắc cho mục đích {purposeType}.")
                : Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _priorityRuleService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.PriorityRuleId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.ActivateAsync(id, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(
            int id,
            CancellationToken cancellationToken)
        {
            await _priorityRuleService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
