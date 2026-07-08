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
    public class LabRoomRepository : BaseRepository<LabRoom>, ILabRoomRepository
    {
        public LabRoomRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<LabRoom?> GetDetailAsync(
            int labId,
            CancellationToken cancellationToken = default)
        {
            return await Context.LabRooms
                .Include(x => x.Manager)
                .Include(x => x.Equipments)
                .Include(x => x.Maintenances)
                .FirstOrDefaultAsync(x => x.LabId == labId, cancellationToken);
        }

        public async Task<LabRoom?> GetByRoomCodeAsync(
            string roomCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedRoomCode = roomCode.Trim().ToLower();

            return await Context.LabRooms
                .Include(x => x.Manager)
                .FirstOrDefaultAsync(
                    x => x.RoomCode.ToLower() == normalizedRoomCode,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<LabRoom>> GetByManagerIdAsync(
            int managerId,
            CancellationToken cancellationToken = default)
        {
            return await Context.LabRooms
                .Include(x => x.Manager)
                .Where(x => x.ManagerId == managerId)
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LabRoom>> GetAvailableLabRoomsAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.LabRooms
                .Include(x => x.Manager)
                .Where(x => x.Status == LabRoomStatus.Available)
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LabRoom>> SearchAsync(
            string? keyword,
            LabRoomStatus? status,
            int? managerId,
            CancellationToken cancellationToken = default)
        {
            var query = Context.LabRooms
                .Include(x => x.Manager)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.LabName.ToLower().Contains(normalizedKeyword)
                    || x.RoomCode.ToLower().Contains(normalizedKeyword)
                    || x.Location.ToLower().Contains(normalizedKeyword));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (managerId.HasValue)
            {
                query = query.Where(x => x.ManagerId == managerId.Value);
            }

            return await query
                .OrderBy(x => x.LabName)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsRoomCodeExistsAsync(
            string roomCode,
            int? excludeLabId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedRoomCode = roomCode.Trim().ToLower();

            return await Context.LabRooms
                .AnyAsync(
                    x => x.RoomCode.ToLower() == normalizedRoomCode
                         && (excludeLabId == null || x.LabId != excludeLabId.Value),
                    cancellationToken);
        }
    }

}
