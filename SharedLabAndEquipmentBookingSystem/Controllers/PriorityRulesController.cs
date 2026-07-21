using Application.DTOs.PriorityRules;
using Domain;
using Application.Features.PriorityRules.Commands.Activate;
using Application.Features.PriorityRules.Commands.Create;
using Application.Features.PriorityRules.Commands.Deactivate;
using Application.Features.PriorityRules.Commands.Update;
using Application.Features.PriorityRules.Queries.GetActive;
using Application.Features.PriorityRules.Queries.GetAll;
using Application.Features.PriorityRules.Queries.GetById;
using Application.Features.PriorityRules.Queries.GetByPurposeType;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PriorityRulesController : ControllerBase
    {
        private readonly ISender _sender;
        public PriorityRulesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new PriorityRuleGetAllQuery(), cancellationToken));

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new PriorityRuleGetActiveQuery(), cancellationToken));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new PriorityRuleGetByIdQuery(id), cancellationToken);
            return result is null
                ? NotFound($"Không tìm thấy quy tắc ưu tiên có ID {id}.")
                : Ok(result);
        }

        [HttpGet("purpose/{purposeType}")]
        public async Task<IActionResult> GetByPurposeType(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new PriorityRuleGetByPurposeTypeQuery(purposeType), cancellationToken);
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
            var result = await _sender.Send(new PriorityRuleCreateCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.PriorityRuleId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new PriorityRuleUpdateCommand(id, request), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new PriorityRuleActivateCommand(id), cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new PriorityRuleDeactivateCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
