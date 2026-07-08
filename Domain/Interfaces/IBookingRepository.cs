using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IBookingRepository : IBaseRepository<Booking>
    {
        Task<Booking?> GetDetailAsync(int bookingId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetByApproverIdAsync(int approverId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetCalendarAsync(
            DateTime from,
            DateTime to,
            int? labId = null,
            int? equipmentId = null,
            CancellationToken cancellationToken = default);

        Task<bool> HasBookingConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId = null,
            bool includePending = true,
            CancellationToken cancellationToken = default);

        Task<int> CountByStatusAsync(
            BookingStatus status,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);
    }

}
