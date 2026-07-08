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
    public class EquipmentRepository : BaseRepository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Equipment?> GetDetailAsync(
            int equipmentId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Equipments
                .Include(x => x.LabRoom)
                .Include(x => x.Maintenances)
                .FirstOrDefaultAsync(x => x.EquipmentId == equipmentId, cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Equipments
                .Include(x => x.LabRoom)
                .Where(x => x.LabId == labId)
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> GetAvailableByLabIdAsync(
            int labId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Equipments
                .Include(x => x.LabRoom)
                .Where(x => x.LabId == labId && x.Status == EquipmentStatus.Available)
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> SearchAsync(
            string? keyword,
            int? labId,
            EquipmentStatus? status,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Equipments
                .Include(x => x.LabRoom)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.EquipmentName.ToLower().Contains(normalizedKeyword)
                    || (x.ModelSpecs != null && x.ModelSpecs.ToLower().Contains(normalizedKeyword)));
            }

            if (labId.HasValue)
            {
                query = query.Where(x => x.LabId == labId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await query
                .OrderBy(x => x.EquipmentName)
                .ToListAsync(cancellationToken);
        }
    }

}
