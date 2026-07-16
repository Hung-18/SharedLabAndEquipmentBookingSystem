using Application.DTOs;
using Application.DTOs.Booking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IBookingService
    {
        Task<List<BookingResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<BookingDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<BookingResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
        Task<List<BookingResponse>> GetPendingAsync(CancellationToken cancellationToken);
        Task<List<BookingResponse>> GetCalendarAsync(
            DateTime from,
            DateTime to,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken);
        Task<BookingDetailResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdateBookingRequest request, CancellationToken cancellationToken);
        Task ApproveAsync(int id, BookingActionRequest request, CancellationToken cancellationToken);
        Task RejectAsync(int id, RejectBookingRequest request, CancellationToken cancellationToken);
        Task CancelAsync(int id, BookingActionRequest request, CancellationToken cancellationToken);
        Task CompleteAsync(int id, BookingActionRequest request, CancellationToken cancellationToken);
        Task MarkNoShowAsync(int id, BookingActionRequest request, CancellationToken cancellationToken);
        Task<PageResult<BookingResponse>> PageResultAsync(int page, int pageSize, CancellationToken cancelation);
    }

}
