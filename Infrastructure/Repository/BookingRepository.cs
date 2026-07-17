using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class BookingRepository : BaseRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Booking>> GetAllDetailedAsync(
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Booking?> GetDetailAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Include(x => x.Violations)
                .FirstOrDefaultAsync(
                    x => x.BookingId == bookingId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.Status == BookingStatus.Pending)
                .OrderBy(x => x.PriorityRule == null
                    ? int.MaxValue
                    : x.PriorityRule.PriorityLevel)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.BookingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetCompetingPendingBookingsAsync(
            int bookingId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            var currentResources = await Context.BookingItems
                .Where(x => x.BookingId == bookingId)
                .Select(x => new
                {
                    x.LabId,
                    x.EquipmentId
                })
                .ToListAsync(cancellationToken);

            var labIds = currentResources
                .Where(x => x.LabId.HasValue)
                .Select(x => x.LabId!.Value)
                .Distinct()
                .ToList();

            var equipmentIds = currentResources
                .Where(x => x.EquipmentId.HasValue)
                .Select(x => x.EquipmentId!.Value)
                .Distinct()
                .ToList();

            var equipmentLabIds = await Context.Equipments
                .Where(x => equipmentIds.Contains(x.EquipmentId))
                .Select(x => x.LabId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await DetailedQuery()
                .Where(x => x.BookingId != bookingId)
                .Where(x => x.Status == BookingStatus.Pending)
                .Where(x => x.StartTime < endTime && x.EndTime > startTime)
                .Where(x => x.BookingItems.Any(item =>
                    (item.LabId.HasValue
                        && (labIds.Contains(item.LabId.Value)
                            || equipmentLabIds.Contains(item.LabId.Value)))
                    || (item.EquipmentId.HasValue
                        && (equipmentIds.Contains(item.EquipmentId.Value)
                            || (item.Equipment != null
                                && labIds.Contains(item.Equipment.LabId))))))
                .OrderBy(x => x.PriorityRule == null
                    ? int.MaxValue
                    : x.PriorityRule.PriorityLevel)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.BookingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByApproverIdAsync(
            int approverId,
            CancellationToken cancellationToken = default)
        {
            return await DetailedQuery()
                .Where(x => x.ApprovedById == approverId)
                .OrderByDescending(x => x.ApprovedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetCalendarAsync(
            DateTime from,
            DateTime to,
            int? labId = null,
            int? equipmentId = null,
            CancellationToken cancellationToken = default)
        {
            var query = DetailedQuery()
                .Where(x =>
                    x.Status != BookingStatus.Cancelled
                    && x.Status != BookingStatus.Rejected
                    && x.StartTime < to
                    && x.EndTime > from);

            if (labId.HasValue)
            {
                int resolvedLabId = labId.Value;
                query = query.Where(x =>
                    x.BookingItems.Any(item =>
                        item.LabId == resolvedLabId
                        || (item.Equipment != null
                            && item.Equipment.LabId == resolvedLabId)));
            }

            if (equipmentId.HasValue)
            {
                int resolvedEquipmentId = equipmentId.Value;
                int? equipmentLabId = await Context.Equipments
                    .Where(x => x.EquipmentId == resolvedEquipmentId)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!equipmentLabId.HasValue)
                {
                    return Array.Empty<Booking>();
                }

                query = query.Where(x =>
                    x.BookingItems.Any(item =>
                        item.EquipmentId == resolvedEquipmentId
                        || item.LabId == equipmentLabId.Value));
            }

            return await query
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.PriorityRule == null
                    ? int.MaxValue
                    : x.PriorityRule.PriorityLevel)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasBookingConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null,
            bool includePending = true,
            CancellationToken cancellationToken = default)
        {
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            var blockingStatuses = includePending
                ? new[] { BookingStatus.Pending, BookingStatus.Approved }
                : new[] { BookingStatus.Approved };

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                equipmentLabId = await Context.Equipments
                    .Where(x => x.EquipmentId == equipmentId.Value)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await Context.BookingItems.AnyAsync(
                x => x.Booking != null
                    && blockingStatuses.Contains(x.Booking.Status)
                    && x.Booking.StartTime < endTime
                    && x.Booking.EndTime > startTime
                    && (!excludeBookingId.HasValue
                        || x.BookingId != excludeBookingId.Value)
                    && (
                        (labId.HasValue
                            && (x.LabId == labId.Value
                                || (x.Equipment != null
                                    && x.Equipment.LabId == labId.Value)))
                        || (equipmentId.HasValue
                            && (x.EquipmentId == equipmentId.Value
                                || (equipmentLabId.HasValue
                                    && x.LabId == equipmentLabId.Value)))
                    ),
                cancellationToken);
        }

        public async Task<int> CountByStatusAsync(
            BookingStatus status,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Bookings
                .Where(x => x.Status == status)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreatedAt <= to.Value);

            return await query.CountAsync(cancellationToken);
        }

        private IQueryable<Booking> DetailedQuery()
        {
            return Context.Bookings
                .Include(x => x.User)
                .Include(x => x.ApprovedBy)
                .Include(x => x.PriorityRule)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment);
        }
    }
}
