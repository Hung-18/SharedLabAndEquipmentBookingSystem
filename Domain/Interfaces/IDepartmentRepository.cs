using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IDepartmentRepository : IBaseRepository<Department>
    {
        Task<Department?> GetByNameAsync(string departmentName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken = default);

        Task<bool> IsDepartmentNameExistsAsync(
            string departmentName,
            int? excludeDepartmentId = null,
            CancellationToken cancellationToken = default);
    }

}
