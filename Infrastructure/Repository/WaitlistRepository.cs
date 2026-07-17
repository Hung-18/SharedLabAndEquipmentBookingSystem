using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class WaitlistRepository : BaseRepository<Waitlist>, IWaitlistRepository
    {
        public WaitlistRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<Waitlist>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Waitlists
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.RequestedStart)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Waitlist>> GetByManagerIdAsync(
            int managerId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Waitlists
                .Include(x => x.User)
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                    .ThenInclude(x => x!.LabRoom)
                .Where(x =>
                    (x.LabRoom != null
                        && x.LabRoom.ManagerId == managerId)
                    || (x.Equipment != null
                        && x.Equipment.LabRoom != null
                        && x.Equipment.LabRoom.ManagerId == managerId))
                .OrderByDescending(x => x.RequestedStart)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Waitlist>> GetWaitingByResourceAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            return await Context.Waitlists
                .Include(x => x.User)
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x =>
                    x.Status == WaitlistStatus.Waiting
                    && x.RequestedStart < requestedEnd
                    && x.RequestedEnd > requestedStart
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue
                            && x.EquipmentId == equipmentId.Value)
                    ))
                .OrderBy(x => x.QueuePosition)
                .ThenBy(x => x.WaitlistId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Waitlist?> GetNextInQueueAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            return await Context.Waitlists
                .Include(x => x.User)
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x =>
                    x.Status == WaitlistStatus.Waiting
                    && x.RequestedStart < requestedEnd
                    && x.RequestedEnd > requestedStart
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue
                            && x.EquipmentId == equipmentId.Value)
                    ))
                .OrderBy(x => x.QueuePosition)
                .ThenBy(x => x.WaitlistId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> GetNextQueuePositionAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            var activeStatuses = new[]
            {
                WaitlistStatus.Waiting,
                WaitlistStatus.Notified
            };

            var maxPosition = await Context.Waitlists
                .Where(x =>
                    activeStatuses.Contains(x.Status)
                    && x.RequestedStart < requestedEnd
                    && x.RequestedEnd > requestedStart
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue
                            && x.EquipmentId == equipmentId.Value)
                    ))
                .Select(x => (int?)x.QueuePosition)
                .MaxAsync(cancellationToken);

            return (maxPosition ?? 0) + 1;
        }

        public async Task<bool> HasUserAlreadyWaitingAsync(
            int userId,
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            var activeStatuses = new[]
            {
                WaitlistStatus.Waiting,
                WaitlistStatus.Notified
            };

            return await Context.Waitlists.AnyAsync(
                x => x.UserId == userId
                    && activeStatuses.Contains(x.Status)
                    && x.RequestedStart < requestedEnd
                    && x.RequestedEnd > requestedStart
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue
                            && x.EquipmentId == equipmentId.Value)
                    ),
                cancellationToken);
        }

        public async Task<Waitlist?> GetActiveReservationAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                equipmentLabId = await Context.Equipments
                    .Where(x => x.EquipmentId == equipmentId.Value)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var candidates = await Context.Waitlists
                .Include(x => x.LabRoom)
                .Include(x => x.Equipment)
                .Where(x =>
                    (x.Status == WaitlistStatus.Notified
                        || x.Status == WaitlistStatus.Booked)
                    && x.RequestedStart < requestedEnd
                    && x.RequestedEnd > requestedStart
                    && (
                        (labId.HasValue
                            && (x.LabId == labId.Value
                                || (x.Equipment != null
                                    && x.Equipment.LabId == labId.Value)))
                        || (equipmentId.HasValue
                            && (x.EquipmentId == equipmentId.Value
                                || (equipmentLabId.HasValue
                                    && x.LabId == equipmentLabId.Value)))
                    ))
                .OrderBy(x => x.Status == WaitlistStatus.Notified ? 0 : 1)
                .ThenBy(x => x.NotifiedAt)
                .ThenBy(x => x.QueuePosition)
                .ThenBy(x => x.WaitlistId)
                .ToListAsync(cancellationToken);

            foreach (var candidate in candidates)
            {
                if (candidate.Status == WaitlistStatus.Notified)
                    return candidate;

                bool hasPendingBooking = await Context.Bookings.AnyAsync(
                    booking =>
                        booking.UserId == candidate.UserId
                        && booking.Status == BookingStatus.Pending
                        && booking.StartTime == candidate.RequestedStart
                        && booking.EndTime == candidate.RequestedEnd
                        && booking.BookingItems.Any(item =>
                            (candidate.LabId.HasValue
                                && item.LabId == candidate.LabId)
                            || (candidate.EquipmentId.HasValue
                                && item.EquipmentId == candidate.EquipmentId)),
                    cancellationToken);

                if (hasPendingBooking)
                    return candidate;
            }

            return null;
        }

    }
}
