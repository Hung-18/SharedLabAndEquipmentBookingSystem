using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IBookingItemRepository : IBaseRepository<BookingItem>
    {
        Task<IReadOnlyList<BookingItem>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BookingItem>> GetByResourceAsync(
            ResourceType resourceType,
            int resourceId,
            CancellationToken cancellationToken = default);

        Task<bool> HasResourceInBookingAsync(
            int bookingId,
            ResourceType resourceType,
            int resourceId,
            CancellationToken cancellationToken = default);
    }

}
