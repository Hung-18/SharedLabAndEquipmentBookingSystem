using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<LabRoom>> GetLabRoomsAsync(
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.LabRooms
                .AsNoTracking()
                .AsQueryable();

            query = ApplyLabScope(query, allowedLabIds);

            return await query
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> GetEquipmentsAsync(
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Equipments
                .AsNoTracking()
                .Include(x => x.LabRoom)
                .AsQueryable();

            if (allowedLabIds is not null)
            {
                var ids = allowedLabIds.ToArray();
                query = query.Where(x => ids.Contains(x.LabId));
            }

            return await query
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetBookingsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x!.Department)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
                        .ThenInclude(x => x!.LabRoom)
                .Where(x => x.StartTime < to && x.EndTime > from)
                .AsQueryable();

            query = ApplyBookingScope(query, allowedLabIds);

            return await query
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.BookingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetUsageLogsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.UsageLogs
                .AsNoTracking()
                .Include(x => x.BookingItem)
                    .ThenInclude(x => x!.Booking)
                        .ThenInclude(x => x!.User)
                            .ThenInclude(x => x!.Department)
                .Include(x => x.BookingItem)
                    .ThenInclude(x => x!.LabRoom)
                .Include(x => x.BookingItem)
                    .ThenInclude(x => x!.Equipment)
                        .ThenInclude(x => x!.LabRoom)
                .Where(x =>
                    x.ActualCheckin < to
                    && (!x.ActualCheckout.HasValue
                        || x.ActualCheckout.Value > from))
                .AsQueryable();

            if (allowedLabIds is not null)
            {
                var ids = allowedLabIds.ToArray();
                query = query.Where(x =>
                    x.BookingItem != null
                    && ((x.BookingItem.LabId.HasValue
                            && ids.Contains(x.BookingItem.LabId.Value))
                        || (x.BookingItem.Equipment != null
                            && ids.Contains(x.BookingItem.Equipment.LabId))));
            }

            return await query
                .OrderBy(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Maintenance>> GetMaintenancesAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Maintenances
                .AsNoTracking()
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                    .ThenInclude(x => x!.LabRoom)
                .Where(x => x.StartTime < to && x.EndTime > from)
                .AsQueryable();

            if (allowedLabIds is not null)
            {
                var ids = allowedLabIds.ToArray();
                query = query.Where(x =>
                    (x.LabId.HasValue && ids.Contains(x.LabId.Value))
                    || (x.Equipment != null && ids.Contains(x.Equipment.LabId)));
            }

            return await query
                .OrderBy(x => x.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<(
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
                CancellationToken cancellationToken = default)
        {
            var query = _context.Maintenances
                .AsNoTracking()
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                    .ThenInclude(x => x!.LabRoom)
                .Include(x => x.CreatedBy)
                .Where(x => x.StartTime < to && x.EndTime > from)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (labId.HasValue)
            {
                query = query.Where(x =>
                    x.LabId == labId.Value
                    || (x.Equipment != null
                        && x.Equipment.LabId == labId.Value));
            }

            if (equipmentId.HasValue)
                query = query.Where(x => x.EquipmentId == equipmentId.Value);

            if (createdById.HasValue)
                query = query.Where(x => x.CreatedById == createdById.Value);

            if (allowedLabIds is not null)
            {
                var ids = allowedLabIds.ToArray();
                query = query.Where(x =>
                    (x.LabId.HasValue
                        && ids.Contains(x.LabId.Value))
                    || (x.Equipment != null
                        && ids.Contains(x.Equipment.LabId)));
            }

            int totalCount = await query.CountAsync(cancellationToken);

            decimal totalCost = await query
                .Where(x => x.Status != MaintenanceStatus.Cancelled)
                .Select(x => (decimal?)x.MaintenanceCost)
                .SumAsync(cancellationToken)
                ?? 0m;

            var items = await query
                .OrderByDescending(x => x.StartTime)
                .ThenByDescending(x => x.MaintenanceId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount, totalCost);
        }

        public async Task<IReadOnlyList<Violation>> GetViolationsAsync(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<int>? allowedLabIds,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Violations
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x!.Department)
                .Include(x => x.Booking)
                    .ThenInclude(x => x!.BookingItems)
                        .ThenInclude(x => x.Equipment)
                .Where(x => x.LoggedAt >= from && x.LoggedAt < to)
                .AsQueryable();

            if (allowedLabIds is not null)
            {
                var ids = allowedLabIds.ToArray();
                query = query.Where(x =>
                    x.Booking != null
                    && x.Booking.BookingItems.Any(item =>
                        (item.LabId.HasValue && ids.Contains(item.LabId.Value))
                        || (item.Equipment != null
                            && ids.Contains(item.Equipment.LabId))));
            }

            return await query
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetUsersWithPenaltyPointsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(x => x.Department)
                .Where(x => x.PenaltyPoints > 0)
                .OrderByDescending(x => x.PenaltyPoints)
                .ThenBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<LabRoom> ApplyLabScope(
            IQueryable<LabRoom> query,
            IReadOnlyCollection<int>? allowedLabIds)
        {
            if (allowedLabIds is null)
                return query;

            var ids = allowedLabIds.ToArray();
            return query.Where(x => ids.Contains(x.LabId));
        }

        private static IQueryable<Booking> ApplyBookingScope(
            IQueryable<Booking> query,
            IReadOnlyCollection<int>? allowedLabIds)
        {
            if (allowedLabIds is null)
                return query;

            var ids = allowedLabIds.ToArray();
            return query.Where(x => x.BookingItems.Any(item =>
                (item.LabId.HasValue && ids.Contains(item.LabId.Value))
                || (item.Equipment != null
                    && ids.Contains(item.Equipment.LabId))));
        }
    }
}
