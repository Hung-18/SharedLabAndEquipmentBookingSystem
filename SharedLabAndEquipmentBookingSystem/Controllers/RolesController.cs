using Application.DTOs.Roles;
using Application.Features.Roles.Queries.GetAll;
using Application.Features.Roles.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly ISender _sender;
        public RolesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new RoleGetAllQuery(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RoleGetByIdQuery(id), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
