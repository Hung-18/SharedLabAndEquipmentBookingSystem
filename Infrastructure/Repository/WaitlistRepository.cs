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
    public class WaitlistRepository : BaseRepository<Waitlist>, IWaitlistRepository
    {
        public WaitlistRepository(ApplicationDbContext context) : base(context)
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
                    && x.RequestedStart == requestedStart
                    && x.RequestedEnd == requestedEnd
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)
                    ))
                .OrderBy(x => x.QueuePosition)
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
                    && x.RequestedStart == requestedStart
                    && x.RequestedEnd == requestedEnd
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)
                    ))
                .OrderBy(x => x.QueuePosition)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> GetNextQueuePositionAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default)
        {
            var maxPosition = await Context.Waitlists
                .Where(x =>
                    x.RequestedStart == requestedStart
                    && x.RequestedEnd == requestedEnd
                    && (
                        (labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)
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

            return await Context.Waitlists
                .AnyAsync(
                    x => x.UserId == userId
                         && activeStatuses.Contains(x.Status)
                         && x.RequestedStart == requestedStart
                         && x.RequestedEnd == requestedEnd
                         && (
                             (labId.HasValue && x.LabId == labId.Value)
                             || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)
                         ),
                    cancellationToken);
        }
    }

}
