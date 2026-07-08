using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IViolationRepository : IBaseRepository<Violation>
    {
        Task<IReadOnlyList<Violation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Violation>> GetByBookingIdAsync(int bookingId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Violation>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<int> GetTotalActivePenaltyPointsAsync(int userId, CancellationToken cancellationToken = default);

        Task<int> CountActiveViolationsAsync(int userId, CancellationToken cancellationToken = default);
    }

}
