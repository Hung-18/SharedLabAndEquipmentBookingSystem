using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<Department>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Departments
                .AsNoTracking()
                .OrderBy(x => x.DepartmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<Department?> GetByNameAsync(
            string departmentName,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = departmentName.Trim().ToLower();

            return await Context.Departments.FirstOrDefaultAsync(
                x => x.DepartmentName.ToLower() == normalizedName,
                cancellationToken);
        }

        public async Task<IReadOnlyList<Department>> GetActiveDepartmentsAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Departments
                .AsNoTracking()
                .Where(x => x.Status == DepartmentStatus.Active)
                .OrderBy(x => x.DepartmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsDepartmentNameExistsAsync(
            string departmentName,
            int? excludeDepartmentId = null,
            CancellationToken cancellationToken = default)
        {
            string normalizedName = departmentName.Trim().ToLower();

            return await Context.Departments.AnyAsync(
                x => x.DepartmentName.ToLower() == normalizedName
                    && (!excludeDepartmentId.HasValue
                        || x.DepartmentId != excludeDepartmentId.Value),
                cancellationToken);
        }
    }
}
