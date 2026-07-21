using Application.DTOs.Departments;
using Application.Features.Departments.Commands.Activate;
using Application.Features.Departments.Commands.Create;
using Application.Features.Departments.Commands.Deactivate;
using Application.Features.Departments.Commands.Update;
using Application.Features.Departments.Queries.GetAll;
using Application.Features.Departments.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly ISender _sender;
        public DepartmentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DepartmentResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new DepartmentGetAllQuery(activeOnly), cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DepartmentGetByIdQuery(id), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(
            [FromBody] CreateDepartmentRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DepartmentCreateCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.DepartmentId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateDepartmentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new DepartmentUpdateCommand(id, request), cancellationToken));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Deactivate(
            int id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new DepartmentDeactivateCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/activate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(DepartmentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new DepartmentActivateCommand(id), cancellationToken));
        }
    }
}
