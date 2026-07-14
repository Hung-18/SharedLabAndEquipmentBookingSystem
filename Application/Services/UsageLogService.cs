using Application.DTOs.UsageLogs;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class UsageLogService : IUsageLogService
    {
        private readonly IUsageLogRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UsageLogService(
            IUsageLogRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UsageLogResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var logs = await _repository.GetAllAsync(
                cancellationToken);

            return logs
                .Select(MapResponse)
                .ToList();
        }

        public async Task<UsageLogResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var log = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            return log is null
                ? null
                : MapResponse(log);
        }

        public async Task<List<UsageLogResponse>> GetByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken)
        {
            var bookingItem =
                await _unitOfWork.BookingItems.GetByIdAsync(
                    bookingItemId,
                    cancellationToken);

            if (bookingItem is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy BookingItem có ID {bookingItemId}.");
            }

            var logs =
                await _repository.GetByBookingItemIdAsync(
                    bookingItemId,
                    cancellationToken);

            return logs
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<UsageLogResponse>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(
                bookingId,
                cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");
            }

            var logs =
                await _repository.GetByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            return logs
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<UsageLogResponse>> GetIncidentLogsAsync(
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken)
        {
            if (from.HasValue
                && to.HasValue
                && from.Value > to.Value)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            var logs =
                await _repository.GetIncidentLogsAsync(
                    from,
                    to,
                    cancellationToken);

            return logs
                .Select(MapResponse)
                .ToList();
        }

        public async Task<UsageLogResponse> CheckInAsync(
            CheckInUsageRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var bookingItem =
                await _unitOfWork.BookingItems.GetByIdAsync(
                    request.BookingItemId,
                    cancellationToken);

            if (bookingItem is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy BookingItem có ID {request.BookingItemId}.");
            }

            var booking =
                await _unitOfWork.Bookings.GetDetailAsync(
                    bookingItem.BookingId,
                    cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingItem.BookingId}.");
            }

            await ValidateActorAsync(
                request.UserId,
                booking,
                cancellationToken);

            if (booking.Status != BookingStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Chỉ booking đã được duyệt mới được check-in.");
            }

            bool hasOpenLog =
                await _repository.HasOpenLogAsync(
                    request.BookingItemId,
                    cancellationToken);

            if (hasOpenLog)
            {
                throw new InvalidOperationException(
                    "BookingItem này đang có một lượt sử dụng chưa checkout.");
            }

            DateTime actualCheckin =
                request.ActualCheckin ?? DateTime.UtcNow;

            var usageLog = new UsageLog(
                request.BookingItemId,
                actualCheckin);

            await _repository.AddAsync(
                usageLog,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(usageLog);
        }

        public async Task<UsageLogResponse> CheckOutAsync(
            int logId,
            CheckOutUsageRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var usageLog = await GetLogOrThrowAsync(
                logId,
                cancellationToken);

            var booking =
                await GetBookingForLogAsync(
                    usageLog,
                    cancellationToken);

            await ValidateActorAsync(
                request.UserId,
                booking,
                cancellationToken);

            DateTime actualCheckout =
                request.ActualCheckout ?? DateTime.UtcNow;

            usageLog.CheckOut(actualCheckout);

            if (actualCheckout > booking.EndTime
                && usageLog.IncidentStatus
                    == UsageIncidentStatus.None)
            {
                usageLog.ReportIncident(
                    UsageIncidentStatus.LateCheckout,
                    "Checkout sau thời gian kết thúc booking.");
            }

            _repository.Update(usageLog);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(usageLog);
        }

        public async Task<UsageLogResponse> ReportIncidentAsync(
            int logId,
            ReportUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var usageLog = await GetLogOrThrowAsync(
                logId,
                cancellationToken);

            var booking =
                await GetBookingForLogAsync(
                    usageLog,
                    cancellationToken);

            await ValidateActorAsync(
                request.UserId,
                booking,
                cancellationToken);

            usageLog.ReportIncident(
                request.IncidentStatus,
                request.IncidentDescription);

            _repository.Update(usageLog);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(usageLog);
        }

        private async Task<UsageLog> GetLogOrThrowAsync(
            int logId,
            CancellationToken cancellationToken)
        {
            var usageLog = await _repository.GetByIdAsync(
                logId,
                cancellationToken);

            if (usageLog is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy UsageLog có ID {logId}.");
            }

            return usageLog;
        }

        private async Task<Booking> GetBookingForLogAsync(
            UsageLog usageLog,
            CancellationToken cancellationToken)
        {
            var bookingItem =
                await _unitOfWork.BookingItems.GetByIdAsync(
                    usageLog.BookingItemId,
                    cancellationToken);

            if (bookingItem is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy BookingItem có ID {usageLog.BookingItemId}.");
            }

            var booking =
                await _unitOfWork.Bookings.GetDetailAsync(
                    bookingItem.BookingId,
                    cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingItem.BookingId}.");
            }

            return booking;
        }

        private async Task ValidateActorAsync(
            int actorUserId,
            Booking booking,
            CancellationToken cancellationToken)
        {
            var actor =
                await _unitOfWork.Users.GetUserByIdAsync(
                    actorUserId,
                    cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {actorUserId}.");
            }

            if (actor.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Người dùng có ID {actorUserId} không hoạt động.");
            }

            bool isBookingOwner =
                booking.UserId == actorUserId;

            bool isManager =
                actor.Role?.RoleName == RoleName.Admin
                || actor.Role?.RoleName == RoleName.LabManager;

            if (!isBookingOwner && !isManager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ chủ booking, Admin hoặc LabManager được thực hiện thao tác này.");
            }
        }

        private static UsageLogResponse MapResponse(
            UsageLog usageLog)
        {
            return new UsageLogResponse
            {
                LogId = usageLog.LogId,
                BookingItemId = usageLog.BookingItemId,
                ActualCheckin = usageLog.ActualCheckin,
                ActualCheckout = usageLog.ActualCheckout,
                IncidentStatus =
                    usageLog.IncidentStatus.ToString(),
                IncidentDescription =
                    usageLog.IncidentDescription
            };
        }
    }

}
