using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IRoleRepository : IBaseRepository<Role>
    {
        Task<IReadOnlyList<Role>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default);

        Task<Role?> GetByNameAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default);

        Task<bool> IsRoleNameExistsAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default);
    }
}
