using Application.DTOs.Waitlists;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IWaitlistService
    {
        Task<List<WaitlistResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<WaitlistResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<List<WaitlistResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<List<WaitlistResponse>> GetQueueAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken);

        Task<WaitlistResponse> CreateAsync(
            CreateWaitlistRequest request,
            CancellationToken cancellationToken);

        Task<WaitlistResponse> NotifyNextAsync(
            NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken);

        Task MarkBookedAsync(
            int id,
            int userId,
            CancellationToken cancellationToken);

        Task CancelAsync(
            int id,
            int userId,
            CancellationToken cancellationToken);

        Task ExpireAsync(
            int id,
            int actorUserId,
            CancellationToken cancellationToken);

        // Gọi từ BookingService sau khi một booking bị hủy.
        Task NotifyNextForCancelledBookingAsync(
            int bookingId,
            CancellationToken cancellationToken);
    }

}
