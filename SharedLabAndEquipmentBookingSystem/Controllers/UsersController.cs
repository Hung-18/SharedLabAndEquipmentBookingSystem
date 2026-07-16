using Application.DTOs.Users;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserManagementService _service;

        public UsersController(IUserManagementService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] string? keyword,
            [FromQuery] RoleName? roleName,
            [FromQuery] int? departmentId,
            [FromQuery] UserStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _service.SearchAsync(
                keyword,
                roleName,
                departmentId,
                status,
                pageNumber,
                pageSize,
                cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.UpdateAsync(id, request, cancellationToken));
        }

        [HttpPut("{id:int}/role")]
        [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeRole(
            int id,
            [FromBody] ChangeUserRoleRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.ChangeRoleAsync(id, request, cancellationToken));
        }

        [HttpPut("{id:int}/department")]
        [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeDepartment(
            int id,
            [FromBody] ChangeUserDepartmentRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.ChangeDepartmentAsync(id, request, cancellationToken));
        }

        [HttpPost("{id:int}/lock")]
        public async Task<IActionResult> Lock(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.LockAsync(id, cancellationToken));
        }

        [HttpPost("{id:int}/unlock")]
        public async Task<IActionResult> Unlock(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.UnlockAsync(id, cancellationToken));
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.DeactivateAsync(id, cancellationToken));
        }

        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.ActivateAsync(id, cancellationToken));
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromBody] SetUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.SetStatusAsync(id, request, cancellationToken));
        }

        [HttpGet("{id:int}/penalty")]
        [ProducesResponseType(typeof(UserPenaltyResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPenalty(
            int id,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetPenaltyAsync(id, cancellationToken));
        }
    }
}
