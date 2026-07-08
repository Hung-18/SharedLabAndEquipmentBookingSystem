using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IMaintenanceRepository : IBaseRepository<Maintenance>
    {
        Task<Maintenance?> GetDetailAsync(int maintenanceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Maintenance>> GetByResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Maintenance>> GetActiveInRangeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Maintenance>> GetByCreatorAsync(
            int createdById,
            CancellationToken cancellationToken = default);

        Task<bool> HasMaintenanceConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeMaintenanceId = null,
            CancellationToken cancellationToken = default);

        Task<bool> HasBookingConflictForMaintenanceAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null,
            bool includePending = true,
            CancellationToken cancellationToken = default);

        Task<decimal> GetTotalMaintenanceCostAsync(
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);
    }

}
