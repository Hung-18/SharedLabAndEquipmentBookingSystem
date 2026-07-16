using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<User?> GetUserByIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            string normalizedUsername = username.Trim().ToLower();

            return await DetailedQuery()
                .FirstOrDefaultAsync(
                    x => x.Username.ToLower() == normalizedUsername,
                    cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            string normalizedEmail = email.Trim().ToLower();

            return await DetailedQuery()
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == normalizedEmail,
                    cancellationToken);
        }

        public async Task<User?> GetWithRoleAndDepartmentAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await GetUserByIdAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.Role != null && x.Role.RoleName == roleName)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByDepartmentAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.DepartmentId == departmentId)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetRestrictedUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.Status == UserStatus.Restricted)
                .OrderBy(x => x.RestrictionUntil)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
            string? keyword,
            RoleName? roleName,
            int? departmentId,
            UserStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = DetailedQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string normalized = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(normalized)
                    || x.Username.ToLower().Contains(normalized)
                    || x.Email.ToLower().Contains(normalized));
            }

            if (roleName.HasValue)
            {
                query = query.Where(x =>
                    x.Role != null
                    && x.Role.RoleName == roleName.Value);
            }

            if (departmentId.HasValue)
                query = query.Where(x => x.DepartmentId == departmentId.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            int totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.UserId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsUsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default)
        {
            string normalizedUsername = username.Trim().ToLower();

            return await Context.Users.AnyAsync(
                x => x.Username.ToLower() == normalizedUsername
                    && (!excludeUserId.HasValue
                        || x.UserId != excludeUserId.Value),
                cancellationToken);
        }

        public async Task<bool> IsEmailExistsAsync(
            string email,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default)
        {
            string normalizedEmail = email.Trim().ToLower();

            return await Context.Users.AnyAsync(
                x => x.Email.ToLower() == normalizedEmail
                    && (!excludeUserId.HasValue
                        || x.UserId != excludeUserId.Value),
                cancellationToken);
        }

        public async Task AddUserAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            await Context.Users.AddAsync(user, cancellationToken);
        }

        private IQueryable<User> DetailedQuery()
        {
            return Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department);
        }
    }
}
