using Application.DTOs.Waitlists;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public WaitlistService(
            IWaitlistRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<WaitlistResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var waitlists = await _repository.GetAllAsync(
                cancellationToken);

            return waitlists
                .Select(MapResponse)
                .ToList();
        }

        public async Task<WaitlistResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var waitlist = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            return waitlist is null
                ? null
                : MapResponse(waitlist);
        }

        public async Task<List<WaitlistResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
            }

            var waitlists = await _repository.GetByUserIdAsync(
                userId,
                cancellationToken);

            return waitlists
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<WaitlistResponse>> GetQueueAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            ValidateTime(requestedStart, requestedEnd);

            await ValidateResourceAsync(
                labId,
                equipmentId,
                cancellationToken);

            var waitlists = await _repository.GetWaitingByResourceAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken);

            return waitlists
                .Select(MapResponse)
                .ToList();
        }

        public async Task<WaitlistResponse> CreateAsync(
            CreateWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            ValidateTime(
                request.RequestedStart,
                request.RequestedEnd);

            await ValidateRequesterAsync(
                request.UserId,
                cancellationToken);

            await ValidateResourceAsync(
                request.LabId,
                request.EquipmentId,
                cancellationToken);

            bool maintenanceConflict =
                await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    null,
                    cancellationToken);

            if (maintenanceConflict)
            {
                throw new InvalidOperationException(
                    "Khung giờ này đang có lịch bảo trì, không thể vào hàng đợi.");
            }

            bool bookingConflict =
                await _unitOfWork.Bookings.HasBookingConflictAsync(
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    null,
                    true,
                    cancellationToken);

            if (!bookingConflict)
            {
                throw new InvalidOperationException(
                    "Khung giờ này đang còn trống. Hãy tạo booking thay vì vào hàng đợi.");
            }

            bool alreadyWaiting =
                await _repository.HasUserAlreadyWaitingAsync(
                    request.UserId,
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    cancellationToken);

            if (alreadyWaiting)
            {
                throw new InvalidOperationException(
                    "Người dùng đã có trong hàng đợi của khung giờ này.");
            }

            int queuePosition =
                await _repository.GetNextQueuePositionAsync(
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    cancellationToken);

            var waitlist = new Waitlist(
                request.UserId,
                request.LabId,
                request.EquipmentId,
                request.RequestedStart,
                request.RequestedEnd,
                queuePosition);

            await _repository.AddAsync(
                waitlist,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(waitlist);
        }

        public async Task<WaitlistResponse> NotifyNextAsync(
            NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            ValidateTime(
                request.RequestedStart,
                request.RequestedEnd);

            await ValidateManagerAsync(
                request.ActorUserId,
                cancellationToken);

            await ValidateResourceAsync(
                request.LabId,
                request.EquipmentId,
                cancellationToken);

            bool bookingConflict =
                await _unitOfWork.Bookings.HasBookingConflictAsync(
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    null,
                    true,
                    cancellationToken);

            if (bookingConflict)
            {
                throw new InvalidOperationException(
                    "Tài nguyên vẫn đang có booking trong khung giờ này.");
            }

            bool maintenanceConflict =
                await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                    request.LabId,
                    request.EquipmentId,
                    request.RequestedStart,
                    request.RequestedEnd,
                    null,
                    cancellationToken);

            if (maintenanceConflict)
            {
                throw new InvalidOperationException(
                    "Tài nguyên vẫn đang có lịch bảo trì trong khung giờ này.");
            }

            var next = await NotifyNextForResourceAsync(
                request.LabId,
                request.EquipmentId,
                request.RequestedStart,
                request.RequestedEnd,
                cancellationToken);

            if (next is null)
            {
                throw new KeyNotFoundException(
                    "Không có người dùng nào đang chờ khung giờ này.");
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(next);
        }

        public async Task MarkBookedAsync(
            int id,
            int userId,
            CancellationToken cancellationToken)
        {
            var waitlist = await GetWaitlistOrThrowAsync(
                id,
                cancellationToken);

            if (waitlist.UserId != userId)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ chủ sở hữu hàng đợi mới được xác nhận đã booking.");
            }

            var bookings = await _unitOfWork.Bookings.GetCalendarAsync(
                waitlist.RequestedStart,
                waitlist.RequestedEnd,
                waitlist.LabId,
                waitlist.EquipmentId,
                cancellationToken);

            bool hasMatchingBooking = bookings.Any(x =>
                x.UserId == waitlist.UserId
                && x.StartTime == waitlist.RequestedStart
                && x.EndTime == waitlist.RequestedEnd
                && (x.Status == BookingStatus.Pending
                    || x.Status == BookingStatus.Approved));

            if (!hasMatchingBooking)
            {
                throw new InvalidOperationException(
                    "Chưa tìm thấy booking tương ứng của người dùng cho khung giờ này.");
            }

            waitlist.MarkBooked();
            _repository.Update(waitlist);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            int userId,
            CancellationToken cancellationToken)
        {
            var waitlist = await GetWaitlistOrThrowAsync(
                id,
                cancellationToken);

            var actor = await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
            }

            bool isOwner = waitlist.UserId == userId;
            bool isManager = actor.Role?.RoleName == RoleName.Admin
                || actor.Role?.RoleName == RoleName.LabManager;

            if (!isOwner && !isManager)
            {
                throw new UnauthorizedAccessException(
                    "Không có quyền hủy bản ghi hàng đợi này.");
            }

            waitlist.Cancel();
            _repository.Update(waitlist);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task ExpireAsync(
            int id,
            int actorUserId,
            CancellationToken cancellationToken)
        {
            await ValidateManagerAsync(
                actorUserId,
                cancellationToken);

            var waitlist = await GetWaitlistOrThrowAsync(
                id,
                cancellationToken);

            waitlist.Expire();
            _repository.Update(waitlist);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task NotifyNextForCancelledBookingAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var booking = await _unitOfWork.Bookings.GetDetailAsync(
                bookingId,
                cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");
            }

            if (booking.Status != BookingStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Chỉ được thông báo hàng đợi sau khi booking đã bị hủy.");
            }

            bool changed = false;

            foreach (var item in booking.BookingItems)
            {
                var next = await NotifyNextForResourceAsync(
                    item.LabId,
                    item.EquipmentId,
                    booking.StartTime,
                    booking.EndTime,
                    cancellationToken);

                if (next is not null)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }

        private async Task<Waitlist?> NotifyNextForResourceAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            bool bookingConflict =
                await _unitOfWork.Bookings.HasBookingConflictAsync(
                    labId,
                    equipmentId,
                    requestedStart,
                    requestedEnd,
                    null,
                    true,
                    cancellationToken);

            if (bookingConflict)
            {
                return null;
            }

            bool maintenanceConflict =
                await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                    labId,
                    equipmentId,
                    requestedStart,
                    requestedEnd,
                    null,
                    cancellationToken);

            if (maintenanceConflict)
            {
                return null;
            }

            var next = await _repository.GetNextInQueueAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken);

            if (next is null)
            {
                return null;
            }

            next.MarkNotified();
            _repository.Update(next);

            string resourceName = labId.HasValue
                ? $"phòng lab ID {labId.Value}"
                : $"thiết bị ID {equipmentId!.Value}";

            var notification = new Notification(
                next.UserId,
                "Khung giờ trong hàng đợi đã có chỗ",
                $"{resourceName} đã trống từ "
                + $"{requestedStart:dd/MM/yyyy HH:mm} đến "
                + $"{requestedEnd:dd/MM/yyyy HH:mm}. "
                + "Hãy tạo booking sớm.",
                NotificationType.WaitlistAvailable);

            await _unitOfWork.Notifications.AddAsync(
                notification,
                cancellationToken);

            return next;
        }

        private async Task ValidateRequesterAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
            }

            if (user.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Người dùng đang không hoạt động hoặc bị hạn chế đặt lịch.");
            }
        }

        private async Task ValidateManagerAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
            }

            if (user.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Người dùng thực hiện thao tác hiện không hoạt động.");
            }

            if (user.Role?.RoleName != RoleName.Admin
                && user.Role?.RoleName != RoleName.LabManager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được thực hiện thao tác này.");
            }
        }

        private async Task ValidateResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            if (labId.HasValue)
            {
                var labRoom = await _unitOfWork.LabRooms.GetByIdAsync(
                    labId.Value,
                    cancellationToken);

                if (labRoom is null)
                {
                    throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {labId.Value}.");
                }

                if (labRoom.Status == LabRoomStatus.Inactive)
                {
                    throw new InvalidOperationException(
                        "Phòng lab đã ngừng hoạt động.");
                }
            }

            if (equipmentId.HasValue)
            {
                var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                    equipmentId.Value,
                    cancellationToken);

                if (equipment is null)
                {
                    throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");
                }

                if (equipment.Status == EquipmentStatus.Retired
                    || equipment.Status == EquipmentStatus.Broken)
                {
                    throw new InvalidOperationException(
                        "Thiết bị đang hỏng hoặc đã ngừng sử dụng.");
                }
            }
        }

        private async Task<Waitlist> GetWaitlistOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var waitlist = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            if (waitlist is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy hàng đợi có ID {id}.");
            }

            return waitlist;
        }

        private static void ValidateTime(
            DateTime requestedStart,
            DateTime requestedEnd)
        {
            if (requestedStart >= requestedEnd)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }
        }

        private static WaitlistResponse MapResponse(
            Waitlist waitlist)
        {
            return new WaitlistResponse
            {
                WaitlistId = waitlist.WaitlistId,
                UserId = waitlist.UserId,
                LabId = waitlist.LabId,
                EquipmentId = waitlist.EquipmentId,
                RequestedStart = waitlist.RequestedStart,
                RequestedEnd = waitlist.RequestedEnd,
                QueuePosition = waitlist.QueuePosition,
                NotifiedAt = waitlist.NotifiedAt,
                Status = waitlist.Status.ToString()
            };
        }
    }

}
