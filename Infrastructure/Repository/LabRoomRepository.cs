using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class LabRoomRepository : BaseRepository<LabRoom>, ILabRoomRepository
    {
        public LabRoomRepository(ApplicationDbContext context) : base(context) { }

        public async Task<LabRoom?> GetDetailAsync(int labId, CancellationToken cancellationToken = default) =>
            await Context.LabRooms
                .Include(x => x.Manager)
                .Include(x => x.Equipments)
                .Include(x => x.Maintenances)
                .FirstOrDefaultAsync(x => x.LabId == labId, cancellationToken);

        public async Task<LabRoom?> GetByRoomCodeAsync(
            string roomCode,
            CancellationToken cancellationToken = default)
        {
            string normalized = roomCode.Trim().ToLower();
            return await Context.LabRooms.Include(x => x.Manager)
                .FirstOrDefaultAsync(
                    x => x.RoomCode.ToLower() == normalized,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<LabRoom>> GetByManagerIdAsync(
            int managerId,
            CancellationToken cancellationToken = default) =>
            await Context.LabRooms.Include(x => x.Manager)
                .Where(x => x.ManagerId == managerId)
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<LabRoom>> GetAvailableLabRoomsAsync(
            CancellationToken cancellationToken = default) =>
            await Context.LabRooms.Include(x => x.Manager)
                .Where(x => x.Status == LabRoomStatus.Available)
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);

        public async Task<(IReadOnlyList<LabRoom> Items, int TotalCount)> SearchAsync(
            string? keyword,
            LabRoomStatus? status,
            int? managerId,
            int? minimumCapacity,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = Context.LabRooms.Include(x => x.Manager).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string value = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.LabName.ToLower().Contains(value)
                    || x.RoomCode.ToLower().Contains(value)
                    || x.Location.ToLower().Contains(value)
                    || (x.Description != null && x.Description.ToLower().Contains(value)));
            }
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            if (managerId.HasValue) query = query.Where(x => x.ManagerId == managerId.Value);
            if (minimumCapacity.HasValue) query = query.Where(x => x.Capacity >= minimumCapacity.Value);

            int total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.LabName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<bool> IsRoomCodeExistsAsync(
            string roomCode,
            int? excludeLabId = null,
            CancellationToken cancellationToken = default)
        {
            string normalized = roomCode.Trim().ToLower();
            return await Context.LabRooms.AnyAsync(
                x => x.RoomCode.ToLower() == normalized
                     && (!excludeLabId.HasValue || x.LabId != excludeLabId.Value),
                cancellationToken);
        }

        public async Task<bool> HasActiveDependenciesAsync(
            int labId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            bool activeBooking = await Context.BookingItems.AnyAsync(item =>
                item.Booking != null
                && item.Booking.Status == BookingStatus.Approved
                && item.Booking.EndTime > now
                && (item.LabId == labId
                    || (item.Equipment != null && item.Equipment.LabId == labId)),
                cancellationToken);
            if (activeBooking) return true;

            bool openUsage = await Context.UsageLogs.AnyAsync(log =>
                log.ActualCheckout == null
                && log.BookingItem != null
                && (log.BookingItem.LabId == labId
                    || (log.BookingItem.Equipment != null
                        && log.BookingItem.Equipment.LabId == labId)),
                cancellationToken);
            if (openUsage) return true;

            bool maintenance = await Context.Maintenances.AnyAsync(x =>
                (x.Status == MaintenanceStatus.Scheduled
                    || x.Status == MaintenanceStatus.InProgress)
                && (x.LabId == labId
                    || (x.Equipment != null && x.Equipment.LabId == labId)),
                cancellationToken);
            if (maintenance) return true;

            return await Context.Waitlists.AnyAsync(x =>
                (x.Status == WaitlistStatus.Waiting || x.Status == WaitlistStatus.Notified)
                && (x.LabId == labId
                    || (x.Equipment != null && x.Equipment.LabId == labId)),
                cancellationToken);
        }
    }
}
