using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IPriorityRuleRepository : IBaseRepository<PriorityRule>
    {
        Task<PriorityRule?> GetByPurposeTypeAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriorityRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);

        Task<bool> IsPurposeTypeExistsAsync(
            BookingPurposeType purposeType,
            int? excludePriorityRuleId = null,
            CancellationToken cancellationToken = default);
    }

}
