using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IDepartmentRepository : IBaseRepository<Department>
    {
        Task<IReadOnlyList<Department>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default);

        Task<Department?> GetByNameAsync(
            string departmentName,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Department>> GetActiveDepartmentsAsync(
            CancellationToken cancellationToken = default);

        Task<bool> IsDepartmentNameExistsAsync(
            string departmentName,
            int? excludeDepartmentId = null,
            CancellationToken cancellationToken = default);
    }
}
