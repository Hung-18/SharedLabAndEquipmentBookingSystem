using Application.DTOs.Roles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleResponse>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<RoleResponse?> GetByIdAsync(
            int roleId,
            CancellationToken cancellationToken = default);
    }

}
