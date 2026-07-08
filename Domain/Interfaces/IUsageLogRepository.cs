using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IUsageLogRepository : IBaseRepository<UsageLog>
    {
        Task<UsageLog?> GetOpenLogByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UsageLog>> GetByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UsageLog>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UsageLog>> GetIncidentLogsAsync(
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);

        Task<bool> HasOpenLogAsync(
            int bookingItemId,
            CancellationToken cancellationToken = default);
    }

}
