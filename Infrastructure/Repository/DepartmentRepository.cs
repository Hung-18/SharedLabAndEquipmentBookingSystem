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
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Department?> GetByNameAsync(
            string departmentName,
            CancellationToken cancellationToken = default)
        {
            var normalizedName = departmentName.Trim().ToLower();

            return await Context.Departments
                .FirstOrDefaultAsync(
                    x => x.DepartmentName.ToLower() == normalizedName,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Department>> GetActiveDepartmentsAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Departments
                .Where(x => x.Status == DepartmentStatus.Active)
                .OrderBy(x => x.DepartmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsDepartmentNameExistsAsync(
            string departmentName,
            int? excludeDepartmentId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedName = departmentName.Trim().ToLower();

            return await Context.Departments
                .AnyAsync(
                    x => x.DepartmentName.ToLower() == normalizedName
                         && (excludeDepartmentId == null || x.DepartmentId != excludeDepartmentId.Value),
                    cancellationToken);
        }
    }

}
