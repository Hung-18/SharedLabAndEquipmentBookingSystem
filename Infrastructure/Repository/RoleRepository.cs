using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<Role>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Roles
                .AsNoTracking()
                .OrderBy(x => x.RoleId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Role?> GetByNameAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default)
        {
            return await Context.Roles
                .FirstOrDefaultAsync(
                    x => x.RoleName == roleName,
                    cancellationToken);
        }

        public async Task<bool> IsRoleNameExistsAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default)
        {
            return await Context.Roles.AnyAsync(
                x => x.RoleName == roleName,
                cancellationToken);
        }
    }
}
