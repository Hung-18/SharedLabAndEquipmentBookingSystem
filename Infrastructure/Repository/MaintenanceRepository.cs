using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class MaintenanceRepository : BaseRepository<Maintenance>, IMaintenanceRepository
    {
        public MaintenanceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Maintenance?> GetDetailAsync(
            int maintenanceId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Maintenances
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                    .ThenInclude(x => x!.LabRoom)
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(
                    x => x.MaintenanceId == maintenanceId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Maintenance>> GetByResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Maintenances
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Include(x => x.CreatedBy)
                .AsQueryable();

            if (labId.HasValue)
                query = query.Where(x => x.LabId == labId.Value);

            if (equipmentId.HasValue)
                query = query.Where(x => x.EquipmentId == equipmentId.Value);

            return await query
                .OrderByDescending(x => x.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Maintenance>> GetActiveInRangeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var activeStatuses = new[]
            {
                MaintenanceStatus.Scheduled,
                MaintenanceStatus.InProgress
            };

            return await Context.Maintenances
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x =>
                    activeStatuses.Contains(x.Status)
                    && x.StartTime < to
                    && x.EndTime > from)
                .OrderBy(x => x.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Maintenance>> GetByCreatorAsync(
            int createdById,
            CancellationToken cancellationToken = default)
        {
            return await Context.Maintenances
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x => x.CreatedById == createdById)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasMaintenanceConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeMaintenanceId = null,
            CancellationToken cancellationToken = default)
        {
            var blockingStatuses = new[]
            {
                MaintenanceStatus.Scheduled,
                MaintenanceStatus.InProgress
            };

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                equipmentLabId = await Context.Equipments
                    .Where(x => x.EquipmentId == equipmentId.Value)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await Context.Maintenances
                .AnyAsync(
                    x => blockingStatuses.Contains(x.Status)
                         && x.StartTime < endTime
                         && x.EndTime > startTime
                         && (excludeMaintenanceId == null
                             || x.MaintenanceId != excludeMaintenanceId.Value)
                         && (
                             (labId.HasValue
                              && x.LabId == labId.Value)
                             || (equipmentId.HasValue
                                 && (x.EquipmentId == equipmentId.Value
                                     || (equipmentLabId.HasValue
                                         && x.LabId == equipmentLabId.Value)))
                         ),
                    cancellationToken);
        }

        public async Task<bool> HasBookingConflictForMaintenanceAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null,
            bool includePending = true,
            CancellationToken cancellationToken = default)
        {
            var blockingStatuses = includePending
                ? new[] { BookingStatus.Pending, BookingStatus.Approved }
                : new[] { BookingStatus.Approved };

            return await Context.BookingItems
                .AnyAsync(
                    x => x.Booking != null
                         && blockingStatuses.Contains(x.Booking.Status)
                         && x.Booking.StartTime < endTime
                         && x.Booking.EndTime > startTime
                         && (excludeBookingId == null
                             || x.BookingId != excludeBookingId.Value)
                         && (
                             (labId.HasValue
                              && (x.LabId == labId.Value
                                  || (x.Equipment != null
                                      && x.Equipment.LabId == labId.Value)))
                             || (equipmentId.HasValue
                                 && x.EquipmentId == equipmentId.Value)
                         ),
                    cancellationToken);
        }

        public async Task<decimal> GetTotalMaintenanceCostAsync(
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Maintenances
                .Where(x => x.Status != MaintenanceStatus.Cancelled)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(x => x.StartTime >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.StartTime <= to.Value);

            return await query.SumAsync(x => x.MaintenanceCost, cancellationToken);
        }
    }
}
