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

        public async Task<bool> HasOpenLogAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default)
        {
            return await Context.UsageLogs
                .AnyAsync(
                    x => x.BookingItemId == bookingItemId && x.ActualCheckout == null,
                    cancellationToken);
        }
    }

}
