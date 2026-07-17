using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IWaitlistRepository : IBaseRepository<Waitlist>
    {
        Task<IReadOnlyList<Waitlist>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Waitlist>> GetByManagerIdAsync(
            int managerId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Waitlist>> GetWaitingByResourceAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default);

        Task<Waitlist?> GetNextInQueueAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default);

        Task<int> GetNextQueuePositionAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default);

        Task<bool> HasUserAlreadyWaitingAsync(
            int userId,
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default);

        Task<Waitlist?> GetActiveReservationAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken = default);
    }

}
