using Application.DTOs.Violations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IViolationService
    {
        Task<List<ViolationResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<ViolationResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<List<ViolationResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<List<ViolationResponse>> GetActiveByUserIdAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<List<ViolationResponse>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken);

        Task<UserViolationSummaryResponse> GetUserSummaryAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<ViolationResponse> CreateAsync(
            CreateViolationRequest request,
            CancellationToken cancellationToken);

        Task ResolveAsync(
            int id,
            ViolationActionRequest request,
            CancellationToken cancellationToken);

        Task CancelAsync(
            int id,
            ViolationActionRequest request,
            CancellationToken cancellationToken);
    }

}
