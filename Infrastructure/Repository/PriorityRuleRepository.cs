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
    public class PriorityRuleRepository : BaseRepository<PriorityRule>, IPriorityRuleRepository
    {
        public PriorityRuleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PriorityRule?> GetByPurposeTypeAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken = default)
        {
            return await Context.PriorityRules
                .FirstOrDefaultAsync(x => x.PurposeType == purposeType, cancellationToken);
        }

        public async Task<IReadOnlyList<PriorityRule>> GetActiveRulesAsync(
            CancellationToken cancellationToken = default)
        {
            return await Context.PriorityRules
                .Where(x => x.Status == PriorityRuleStatus.Active)
                .OrderBy(x => x.PriorityLevel)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsPurposeTypeExistsAsync(
            BookingPurposeType purposeType,
            int? excludePriorityRuleId = null,
            CancellationToken cancellationToken = default)
        {
            return await Context.PriorityRules
                .AnyAsync(
                    x => x.PurposeType == purposeType
                         && (excludePriorityRuleId == null || x.PriorityRuleId != excludePriorityRuleId.Value),
                    cancellationToken);
        }
    }

}
