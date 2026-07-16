using Application.DTOs.PriorityRules;
using Domain;

namespace Application.Interfaces
{
    public interface IPriorityRuleService
    {
        Task<List<PriorityRuleResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<List<PriorityRuleResponse>> GetActiveAsync(CancellationToken cancellationToken);
        Task<PriorityRuleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<PriorityRuleResponse?> GetByPurposeTypeAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken);
        Task<PriorityRuleResponse> CreateAsync(
            CreatePriorityRuleRequest request,
            CancellationToken cancellationToken);
        Task UpdateAsync(
            int id,
            UpdatePriorityRuleRequest request,
            CancellationToken cancellationToken);
        Task ActivateAsync(int id, CancellationToken cancellationToken);
        Task DeactivateAsync(int id, CancellationToken cancellationToken);
    }
}
