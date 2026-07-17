using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUsageLogService
    {
        Task<List<UsageLogResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<UsageLogResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<List<UsageLogResponse>> GetByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken);

        Task<List<UsageLogResponse>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken);

        Task<List<UsageLogResponse>> GetIncidentLogsAsync(
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken);

        Task<UsageLogResponse> CheckInAsync(
            CheckInUsageRequest request,
            CancellationToken cancellationToken);

        Task<UsageLogResponse> CheckOutAsync(
            int logId,
            CheckOutUsageRequest request,
            CancellationToken cancellationToken);

        Task<UsageLogResponse> ReportIncidentAsync(
            int logId,
            ReportUsageIncidentRequest request,
            CancellationToken cancellationToken);

        Task<UsageLogResponse> ConfirmIncidentAsync(
            int logId,
            ReviewUsageIncidentRequest request,
            CancellationToken cancellationToken);

        Task<UsageLogResponse> RejectIncidentAsync(
            int logId,
            ReviewUsageIncidentRequest request,
            CancellationToken cancellationToken);
    }

}
