using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetWithRoleAndDepartmentAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetByRoleAsync(RoleName roleName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetRestrictedUsersAsync(CancellationToken cancellationToken = default);

        Task<bool> IsUsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default);

        Task<bool> IsEmailExistsAsync(
            string email,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default);
    }

}
