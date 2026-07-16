using Application.DTOs.Waitlists;

namespace Application.Interfaces
{
    public interface IWaitlistService
    {
        Task<List<WaitlistResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<WaitlistResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<WaitlistResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
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
        Task MarkBookedAsync(int id, CancellationToken cancellationToken);
        Task CancelAsync(int id, CancellationToken cancellationToken);
        Task ExpireAsync(int id, CancellationToken cancellationToken);
        Task NotifyNextForCancelledBookingAsync(
            int bookingId,
            CancellationToken cancellationToken);
        Task NotifyNextForReleasedBookingAsync(
            int bookingId,
            CancellationToken cancellationToken);

        Task<int> ProcessExpiredNotificationsAsync(
            DateTime expiredBefore,
            CancellationToken cancellationToken);
    }
}
