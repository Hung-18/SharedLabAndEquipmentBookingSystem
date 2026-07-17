using Domain;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IReportRepository
    {
        Task<IReadOnlyList<LabRoom>> GetLabRoomsAsync(
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Equipment>> GetEquipmentsAsync(
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetBookingsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UsageLog>> GetUsageLogsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Maintenance>> GetMaintenancesAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<(
            IReadOnlyList<Maintenance> Items,
            int TotalCount,
            decimal TotalCost)> GetMaintenanceHistoryAsync(
                DateTime from,
                DateTime to,
                MaintenanceStatus? status,
                int? labId,
                int? equipmentId,
                int? createdById,
                IReadOnlyCollection<int>? allowedLabIds,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Violation>> GetViolationsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetUsersWithPenaltyPointsAsync(
            CancellationToken cancellationToken = default);
    }
}
