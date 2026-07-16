using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IEquipmentRepository : IBaseRepository<Equipment>
    {
        Task<Equipment?> GetDetailAsync(int equipmentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Equipment>> GetByLabIdAsync(int labId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Equipment>> GetAvailableByLabIdAsync(int labId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Equipment> Items, int TotalCount)> SearchAsync(
            string? keyword,
            int? labId,
            EquipmentStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<bool> HasActiveDependenciesAsync(
            int equipmentId,
            DateTime now,
            CancellationToken cancellationToken = default);
    }
}
