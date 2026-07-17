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
    public class ViolationRepository : BaseRepository<Violation>, IViolationRepository
    {
        public ViolationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Violation>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Violations
                .Include(x => x.Booking)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Violation>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Violations
                .Include(x => x.User)
                .Where(x => x.BookingId == bookingId)
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Violation>> GetActiveByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Violations
                .Include(x => x.Booking)
                .Where(x => x.UserId == userId && x.Status == ViolationStatus.Active)
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Violation>> GetByManagerIdAsync(
            int managerId,
            int? userId = null,
            bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Violations
                .Include(x => x.User)
                .Include(x => x.Booking)
                    .ThenInclude(x => x!.BookingItems)
                        .ThenInclude(x => x.LabRoom)
                .Include(x => x.Booking)
                    .ThenInclude(x => x!.BookingItems)
                        .ThenInclude(x => x.Equipment)
                            .ThenInclude(x => x!.LabRoom)
                .Where(x =>
                    x.Booking != null
                    && x.Booking.BookingItems.Any()
                    && x.Booking.BookingItems.All(item =>
                        (item.LabRoom != null
                            && item.LabRoom.ManagerId == managerId)
                        || (item.Equipment != null
                            && item.Equipment.LabRoom != null
                            && item.Equipment.LabRoom.ManagerId == managerId)));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            if (activeOnly)
                query = query.Where(x => x.Status == ViolationStatus.Active);

            return await query
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetTotalActivePenaltyPointsAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Violations
                .Where(x => x.UserId == userId && x.Status == ViolationStatus.Active)
                .SumAsync(x => x.PenaltyPointsAdded, cancellationToken);
        }

        public async Task<int> CountActiveViolationsAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Violations
                .CountAsync(
                    x => x.UserId == userId && x.Status == ViolationStatus.Active,
                    cancellationToken);
        }
    }

}
