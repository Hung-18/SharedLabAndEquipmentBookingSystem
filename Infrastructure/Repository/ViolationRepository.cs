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
