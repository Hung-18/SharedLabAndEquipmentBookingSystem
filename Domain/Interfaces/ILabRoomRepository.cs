using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ILabRoomRepository : IBaseRepository<LabRoom>
    {
        Task<LabRoom?> GetDetailAsync(int labId, CancellationToken cancellationToken = default);
        Task<LabRoom?> GetByRoomCodeAsync(string roomCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LabRoom>> GetByManagerIdAsync(int managerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LabRoom>> GetAvailableLabRoomsAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<LabRoom> Items, int TotalCount)> SearchAsync(
            string? keyword,
            LabRoomStatus? status,
            int? managerId,
            int? minimumCapacity,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<bool> IsRoomCodeExistsAsync(
            string roomCode,
            int? excludeLabId = null,
            CancellationToken cancellationToken = default);
        Task<bool> HasActiveDependenciesAsync(
            int labId,
            DateTime now,
            CancellationToken cancellationToken = default);
    }
}
