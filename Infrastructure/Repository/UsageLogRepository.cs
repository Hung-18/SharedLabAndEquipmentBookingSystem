using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class UsageLogRepository : BaseRepository<UsageLog>, IUsageLogRepository
    {
        public UsageLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UsageLog?> GetOpenLogByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default)
        {
            return await Context.UsageLogs
                .Include(x => x.BookingItem)
                .FirstOrDefaultAsync(
                    x => x.BookingItemId == bookingItemId && x.ActualCheckout == null,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default)
        {
            return await Context.UsageLogs
                .Include(x => x.BookingItem)
                .Where(x => x.BookingItemId == bookingItemId)
                .OrderByDescending(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            return await Context.UsageLogs
                .Include(x => x.BookingItem)
                .Where(x => x.BookingItem != null && x.BookingItem.BookingId == bookingId)
                .OrderByDescending(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetIncidentLogsAsync(
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = Context.UsageLogs
                .Include(x => x.BookingItem)
                .Where(x => x.IncidentStatus != UsageIncidentStatus.None)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(x => x.ActualCheckin >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.ActualCheckin <= to.Value);
            }

            return await query
                .OrderByDescending(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetByManagerIdAsync(
            int managerId,
            CancellationToken cancellationToken = default)
        {
            return await BuildManagerScopeQuery(managerId)
                .OrderByDescending(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UsageLog>> GetIncidentLogsByManagerIdAsync(
            int managerId,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildManagerScopeQuery(managerId)
                .Where(x => x.IncidentStatus != UsageIncidentStatus.None);

            if (from.HasValue)
                query = query.Where(x => x.ActualCheckin >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.ActualCheckin <= to.Value);

            return await query
                .OrderByDescending(x => x.ActualCheckin)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<UsageLog> BuildManagerScopeQuery(int managerId)
        {
            return Context.UsageLogs
                .Include(x => x.BookingItem)
                    .ThenInclude(x => x!.LabRoom)
                .Include(x => x.BookingItem)
                    .ThenInclude(x => x!.Equipment)
                        .ThenInclude(x => x!.LabRoom)
                .Include(x => x.AffectedEquipment)
                .Where(x =>
                    x.BookingItem != null
                    && ((x.BookingItem.LabRoom != null
                            && x.BookingItem.LabRoom.ManagerId == managerId)
                        || (x.BookingItem.Equipment != null
                            && x.BookingItem.Equipment.LabRoom != null
                            && x.BookingItem.Equipment.LabRoom.ManagerId == managerId)));
        }

        public async Task<bool> HasOpenLogAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default)
        {
            return await Context.UsageLogs
                .AnyAsync(
                    x => x.BookingItemId == bookingItemId && x.ActualCheckout == null,
                    cancellationToken);
        }

        public async Task<bool> HasOpenLogForResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken = default)
        {
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            if (labId.HasValue)
            {
                int targetLabId = labId.Value;

                return await Context.UsageLogs
                    .AnyAsync(
                        x => x.ActualCheckout == null
                            && x.BookingItem != null
                            && (x.BookingItem.LabId == targetLabId
                                || (x.BookingItem.Equipment != null
                                    && x.BookingItem.Equipment.LabId == targetLabId)),
                        cancellationToken);
            }

            int targetEquipmentId = equipmentId!.Value;
            int? equipmentLabId = await Context.Equipments
                .Where(x => x.EquipmentId == targetEquipmentId)
                .Select(x => (int?)x.LabId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!equipmentLabId.HasValue)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {targetEquipmentId}.");
            }

            return await Context.UsageLogs
                .AnyAsync(
                    x => x.ActualCheckout == null
                        && x.BookingItem != null
                        && (x.BookingItem.EquipmentId == targetEquipmentId
                            || x.BookingItem.LabId == equipmentLabId.Value),
                    cancellationToken);
        }
    }

}
