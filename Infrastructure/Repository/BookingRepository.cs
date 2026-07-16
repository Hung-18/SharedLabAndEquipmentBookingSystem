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
    public class BookingRepository : BaseRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Booking?> GetDetailAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Bookings
                .Include(x => x.User)
                .Include(x => x.ApprovedBy)
                .Include(x => x.PriorityRule)
                .Include(x => x.Violations)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Bookings
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.Bookings
                .Include(x => x.User)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
                .Where(x => x.Status == BookingStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByApproverIdAsync(
            int approverId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Bookings
                .Include(x => x.User)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
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
            var query = Context.Bookings
                .Include(x => x.User)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.LabRoom)
                .Include(x => x.BookingItems)
                    .ThenInclude(x => x.Equipment)
                .Where(x =>
                    x.Status != BookingStatus.Cancelled
                    && x.Status != BookingStatus.Rejected
                    && x.StartTime < to
                    && x.EndTime > from)
                .AsQueryable();

            if (labId.HasValue)
            {
                query = query.Where(x => x.BookingItems.Any(item => item.LabId == labId.Value));
            }

            if (equipmentId.HasValue)
            {
                query = query.Where(x => x.BookingItems.Any(item => item.EquipmentId == equipmentId.Value));
            }

            return await query
                .OrderBy(x => x.StartTime)
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
            var blockingStatuses = includePending
                ? new[] { BookingStatus.Pending, BookingStatus.Approved }
                : new[] { BookingStatus.Approved };

            return await Context.BookingItems
                .Include(x => x.Booking)
                .AnyAsync(
                    x => x.Booking != null
                         && blockingStatuses.Contains(x.Booking.Status)
                         && x.Booking.StartTime < endTime
                         && x.Booking.EndTime > startTime
                         && (excludeBookingId == null || x.BookingId != excludeBookingId.Value)
                         && (
                             (labId.HasValue && x.LabId == labId.Value)
                             || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)
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
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= to.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<Booking>> PageResultAsync(int? userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            return await Context.Bookings
                                .AsNoTracking()
                                .Where(u => u.UserId == userId)
                                .OrderByDescending(t => t.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();
        }

        public async Task<int> CountPageAsync(int? userId)
        {
            return await Context.Bookings
                                .CountAsync(c => c.UserId == userId);
        }
    }

}
