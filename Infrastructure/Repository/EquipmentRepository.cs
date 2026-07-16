using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class EquipmentRepository : BaseRepository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Equipment?> GetDetailAsync(
            int equipmentId,
            CancellationToken cancellationToken = default) =>
            await Context.Equipments
                .Include(x => x.LabRoom)
                .Include(x => x.Maintenances)
                .FirstOrDefaultAsync(x => x.EquipmentId == equipmentId, cancellationToken);

        public async Task<IReadOnlyList<Equipment>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken = default) =>
            await Context.Equipments.Include(x => x.LabRoom)
                .Where(x => x.LabId == labId)
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Equipment>> GetAvailableByLabIdAsync(
            int labId,
            CancellationToken cancellationToken = default) =>
            await Context.Equipments.Include(x => x.LabRoom)
                .Where(x => x.LabId == labId && x.Status == EquipmentStatus.Available)
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);

        public async Task<(IReadOnlyList<Equipment> Items, int TotalCount)> SearchAsync(
            string? keyword,
            int? labId,
            EquipmentStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Equipments.Include(x => x.LabRoom).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string value = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.EquipmentName.ToLower().Contains(value)
                    || (x.ModelSpecs != null && x.ModelSpecs.ToLower().Contains(value)));
            }
            if (labId.HasValue) query = query.Where(x => x.LabId == labId.Value);
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);

            int total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.EquipmentName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<bool> HasActiveDependenciesAsync(
            int equipmentId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            bool activeBooking = await Context.BookingItems.AnyAsync(item =>
                item.EquipmentId == equipmentId
                && item.Booking != null
                && item.Booking.Status == BookingStatus.Approved
                && item.Booking.EndTime > now,
                cancellationToken);
            if (activeBooking) return true;

            bool openUsage = await Context.UsageLogs.AnyAsync(log =>
                log.ActualCheckout == null
                && log.BookingItem != null
                && log.BookingItem.EquipmentId == equipmentId,
                cancellationToken);
            if (openUsage) return true;

            bool maintenance = await Context.Maintenances.AnyAsync(x =>
                x.EquipmentId == equipmentId
                && (x.Status == MaintenanceStatus.Scheduled
                    || x.Status == MaintenanceStatus.InProgress),
                cancellationToken);
            if (maintenance) return true;

            return await Context.Waitlists.AnyAsync(x =>
                x.EquipmentId == equipmentId
                && (x.Status == WaitlistStatus.Waiting
                    || x.Status == WaitlistStatus.Notified),
                cancellationToken);
        }
    }
}
