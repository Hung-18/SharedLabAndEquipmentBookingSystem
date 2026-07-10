using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetUserByIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            var normalizedUsername = username.Trim().ToLower();

            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(
                    x => x.Username.ToLower() == normalizedUsername,
                    cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == normalizedEmail,
                    cancellationToken);
        }

        public async Task<User?> GetWithRoleAndDepartmentAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(
            RoleName roleName,
            CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .Where(x => x.Role != null && x.Role.RoleName == roleName)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByDepartmentAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .Where(x => x.DepartmentId == departmentId)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetRestrictedUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .Include(x => x.Role)
                .Include(x => x.Department)
                .Where(x => x.Status == UserStatus.Restricted)
                .OrderBy(x => x.RestrictionUntil)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsUsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedUsername = username.Trim().ToLower();

            return await Context.Users
                .AnyAsync(
                    x => x.Username.ToLower() == normalizedUsername
                         && (excludeUserId == null || x.UserId != excludeUserId.Value),
                    cancellationToken);
        }

        public async Task<bool> IsEmailExistsAsync(
            string email,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await Context.Users
                .AnyAsync(
                    x => x.Email.ToLower() == normalizedEmail
                         && (excludeUserId == null || x.UserId != excludeUserId.Value),
                    cancellationToken);
        }

        public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            await Context.Users.AddAsync(user, cancellationToken);
        }
    }
}
