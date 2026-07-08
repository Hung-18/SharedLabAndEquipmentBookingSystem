using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IRoleRepository : IBaseRepository<Role>
    {
        Task<Role?> GetByNameAsync(RoleName roleName, CancellationToken cancellationToken = default);

        Task<bool> IsRoleNameExistsAsync(RoleName roleName, CancellationToken cancellationToken = default);
    }

}
