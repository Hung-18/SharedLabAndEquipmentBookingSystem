using Application.DTOs.Departments;

namespace Application.Interfaces
{
    public interface IDepartmentService
    {
        Task<List<DepartmentResponse>> GetAllAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default);

        Task<DepartmentResponse?> GetByIdAsync(
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<DepartmentResponse> CreateAsync(
            CreateDepartmentRequest request,
            CancellationToken cancellationToken = default);

        Task<DepartmentResponse> UpdateAsync(
            int departmentId,
            UpdateDepartmentRequest request,
            CancellationToken cancellationToken = default);

        Task DeactivateAsync(
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<DepartmentResponse> ActivateAsync(
            int departmentId,
            CancellationToken cancellationToken = default);
    }
}
