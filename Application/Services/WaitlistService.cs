using Application.DTOs.Waitlists;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public WaitlistService(
            IWaitlistRepository repository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<List<WaitlistResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            var waitlists = await _repository.GetAllAsync(cancellationToken);
            var result = new List<WaitlistResponse>();

            foreach (var waitlist in waitlists)
            {
                if (actor.Role?.RoleName == RoleName.Admin
                    || await CanManageResourceAsync(
                        actor.UserId,
                        waitlist.LabId,
                        waitlist.EquipmentId,
                        cancellationToken))
                {
                    result.Add(MapResponse(waitlist));
                }
            }

            return result;
        }

        public async Task<WaitlistResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var waitlist = await _repository.GetByIdAsync(id, cancellationToken);

            if (waitlist is null)
                return null;

            var canRead = waitlist.UserId == actor.UserId
                || actor.Role?.RoleName == RoleName.Admin
                || (actor.Role?.RoleName == RoleName.LabManager
                    && await CanManageResourceAsync(
                        actor.UserId,
                        waitlist.LabId,
                        waitlist.EquipmentId,
                        cancellationToken));

            if (!canRead)
                throw new UnauthorizedAccessException("Bạn không có quyền xem hàng đợi này.");

            return MapResponse(waitlist);
        }

        public async Task<List<WaitlistResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);

            if (actor.UserId != userId && actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException("Bạn chỉ được xem hàng đợi của chính mình.");

            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");

            var waitlists = await _repository.GetByUserIdAsync(user.UserId, cancellationToken);
            return waitlists.Select(MapResponse).ToList();
        }

        public async Task<List<WaitlistResponse>> GetQueueAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);
            ValidateTime(requestedStart, requestedEnd, requireFuture: false);
            await ValidateResourceAsync(labId, equipmentId, cancellationToken);

            if (actor.Role?.RoleName == RoleName.LabManager
                && !await CanManageResourceAsync(actor.UserId, labId, equipmentId, cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được xem hàng đợi của phòng mình quản lý.");
            }

            var waitlists = await _repository.GetWaitingByResourceAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken);

            return waitlists.Select(MapResponse).ToList();
        }

        public async Task<WaitlistResponse> CreateAsync(
            CreateWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.RequestedStart, request.RequestedEnd, requireFuture: true);

            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            await EnsureCanCreateWaitlistAsync(actor, cancellationToken);
            await ValidateResourceAsync(request.LabId, request.EquipmentId, cancellationToken);

            var maintenanceConflict = await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.RequestedStart,
                request.RequestedEnd,
                null,
                cancellationToken);

            if (maintenanceConflict)
                throw new InvalidOperationException(
                    "Khung giờ này đang có lịch bảo trì, không thể vào hàng đợi.");

            var approvedConflict = await _unitOfWork.Bookings.HasBookingConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.RequestedStart,
                request.RequestedEnd,
                null,
                includePending: false,
                cancellationToken: cancellationToken);

            if (!approvedConflict)
                throw new InvalidOperationException(
                    "Khung giờ này chưa bị booking Approved khóa. Hãy tạo booking Pending thay vì vào hàng đợi.");

            Waitlist? waitlist = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var alreadyWaiting = await _repository.HasUserAlreadyWaitingAsync(
                        actor.UserId,
                        request.LabId,
                        request.EquipmentId,
                        request.RequestedStart,
                        request.RequestedEnd,
                        ct);

                    if (alreadyWaiting)
                        throw new InvalidOperationException(
                            "Bạn đã có trong hàng đợi của khung giờ này.");

                    var queuePosition = await _repository.GetNextQueuePositionAsync(
                        request.LabId,
                        request.EquipmentId,
                        request.RequestedStart,
                        request.RequestedEnd,
                        ct);

                    waitlist = new Waitlist(
                        actor.UserId,
                        request.LabId,
                        request.EquipmentId,
                        request.RequestedStart,
                        request.RequestedEnd,
                        queuePosition);

                    await _repository.AddAsync(waitlist, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            return MapResponse(waitlist!);
        }

        public async Task<WaitlistResponse> NotifyNextAsync(
            NotifyNextWaitlistRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.RequestedStart, request.RequestedEnd, requireFuture: false);

            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);
            await ValidateResourceAsync(request.LabId, request.EquipmentId, cancellationToken);

            if (actor.Role?.RoleName == RoleName.LabManager
                && !await CanManageResourceAsync(
                    actor.UserId,
                    request.LabId,
                    request.EquipmentId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được thao tác với phòng mình quản lý.");
            }

            Waitlist? next = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    next = await NotifyNextForResourceAsync(
                        request.LabId,
                        request.EquipmentId,
                        request.RequestedStart,
                        request.RequestedEnd,
                        ct);

                    if (next is null)
                        throw new KeyNotFoundException(
                            "Không có người dùng nào đang chờ hoặc tài nguyên chưa thực sự trống.");

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            return MapResponse(next!);
        }

        public async Task MarkBookedAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var waitlist = await GetWaitlistOrThrowAsync(id, cancellationToken);

            if (waitlist.UserId != actor.UserId)
                throw new UnauthorizedAccessException(
                    "Chỉ chủ sở hữu hàng đợi mới được xác nhận đã booking.");

            var bookings = await _unitOfWork.Bookings.GetCalendarAsync(
                waitlist.RequestedStart,
                waitlist.RequestedEnd,
                null,
                null,
                cancellationToken);

            var matchingBookingExists = bookings.Any(booking =>
                booking.UserId == actor.UserId
                && booking.StartTime == waitlist.RequestedStart
                && booking.EndTime == waitlist.RequestedEnd
                && booking.Status is BookingStatus.Pending or BookingStatus.Approved
                && booking.BookingItems.Any(item =>
                    (waitlist.LabId.HasValue && item.LabId == waitlist.LabId)
                    || (waitlist.EquipmentId.HasValue
                        && item.EquipmentId == waitlist.EquipmentId)));

            if (!matchingBookingExists)
                throw new InvalidOperationException(
                    "Chưa tìm thấy booking tương ứng để chuyển hàng đợi sang Booked.");

            waitlist.MarkBooked();
            _repository.Update(waitlist);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            var waitlist = await GetWaitlistOrThrowAsync(id, cancellationToken);

            var canCancel = waitlist.UserId == actor.UserId
                || actor.Role?.RoleName == RoleName.Admin
                || (actor.Role?.RoleName == RoleName.LabManager
                    && await CanManageResourceAsync(
                        actor.UserId,
                        waitlist.LabId,
                        waitlist.EquipmentId,
                        cancellationToken));

            if (!canCancel)
                throw new UnauthorizedAccessException(
                    "Không có quyền hủy bản ghi hàng đợi này.");

            var shouldNotifyNext = waitlist.Status == WaitlistStatus.Notified;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    waitlist.Cancel();
                    _repository.Update(waitlist);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            waitlist.UserId,
                            "Hàng đợi đã bị hủy",
                            $"Hàng đợi #{waitlist.WaitlistId} đã bị hủy.",
                            NotificationType.System),
                        ct);

                    if (shouldNotifyNext)
                    {
                        await NotifyNextForResourceAsync(
                            waitlist.LabId,
                            waitlist.EquipmentId,
                            waitlist.RequestedStart,
                            waitlist.RequestedEnd,
                            ct);
                    }

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task ExpireAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);
            var waitlist = await GetWaitlistOrThrowAsync(id, cancellationToken);

            if (actor.Role?.RoleName == RoleName.LabManager
                && !await CanManageResourceAsync(
                    actor.UserId,
                    waitlist.LabId,
                    waitlist.EquipmentId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được thao tác với phòng mình quản lý.");
            }

            var shouldNotifyNext = waitlist.Status == WaitlistStatus.Notified;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    waitlist.Expire();
                    _repository.Update(waitlist);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            waitlist.UserId,
                            "Quyền ưu tiên hàng đợi đã hết hạn",
                            $"Hàng đợi #{waitlist.WaitlistId} đã hết hạn.",
                            NotificationType.System),
                        ct);

                    if (shouldNotifyNext)
                    {
                        await NotifyNextForResourceAsync(
                            waitlist.LabId,
                            waitlist.EquipmentId,
                            waitlist.RequestedStart,
                            waitlist.RequestedEnd,
                            ct);
                    }

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task NotifyNextForCancelledBookingAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var booking = await _unitOfWork.Bookings.GetDetailAsync(
                bookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");

            if (booking.Status != BookingStatus.Cancelled)
                throw new InvalidOperationException(
                    "Booking phải ở trạng thái Cancelled trước khi giải phóng waitlist.");

            await NotifyForReleasedBookingCoreAsync(booking, cancellationToken);
        }

        public async Task NotifyNextForReleasedBookingAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var booking = await _unitOfWork.Bookings.GetDetailAsync(
                bookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");

            if (booking.Status is not BookingStatus.Cancelled
                and not BookingStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Chỉ booking Cancelled hoặc Completed mới giải phóng waitlist.");
            }

            await NotifyForReleasedBookingCoreAsync(booking, cancellationToken);
        }

        public async Task<int> ProcessExpiredNotificationsAsync(
            DateTime expiredBefore,
            CancellationToken cancellationToken)
        {
            int expiredCount = 0;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var expiredItems = await _repository.FindAsync(
                        x => x.Status == WaitlistStatus.Notified
                            && x.NotifiedAt.HasValue
                            && x.NotifiedAt.Value <= expiredBefore,
                        ct);

                    foreach (var item in expiredItems)
                    {
                        item.Expire();
                        _repository.Update(item);
                        expiredCount++;

                        await _unitOfWork.Notifications.AddAsync(
                            new Notification(
                                item.UserId,
                                "Lượt nhận chỗ trong hàng đợi đã hết hạn",
                                $"Bạn đã không tạo booking kịp thời cho khung "
                                + $"{item.RequestedStart:dd/MM/yyyy HH:mm} - "
                                + $"{item.RequestedEnd:dd/MM/yyyy HH:mm}.",
                                NotificationType.WaitlistAvailable),
                            ct);

                        await NotifyNextForResourceAsync(
                            item.LabId,
                            item.EquipmentId,
                            item.RequestedStart,
                            item.RequestedEnd,
                            ct);
                    }

                    if (expiredCount > 0)
                        await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            return expiredCount;
        }

        private async Task NotifyForReleasedBookingCoreAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var resourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in booking.BookingItems)
                {
                    if (item.LabId.HasValue)
                    {
                        int labId = item.LabId.Value;
                        if (resourceKeys.Add($"LAB:{labId}"))
                        {
                            await NotifyNextForResourceAsync(
                                labId,
                                null,
                                booking.StartTime,
                                booking.EndTime,
                                ct);
                        }

                        var equipments = await _unitOfWork.Equipments.GetByLabIdAsync(
                            labId,
                            ct);
                        foreach (var equipment in equipments)
                        {
                            if (resourceKeys.Add($"EQUIPMENT:{equipment.EquipmentId}"))
                            {
                                await NotifyNextForResourceAsync(
                                    null,
                                    equipment.EquipmentId,
                                    booking.StartTime,
                                    booking.EndTime,
                                    ct);
                            }
                        }
                    }
                    else if (item.EquipmentId.HasValue)
                    {
                        int equipmentId = item.EquipmentId.Value;
                        if (resourceKeys.Add($"EQUIPMENT:{equipmentId}"))
                        {
                            await NotifyNextForResourceAsync(
                                null,
                                equipmentId,
                                booking.StartTime,
                                booking.EndTime,
                                ct);
                        }

                        var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                            equipmentId,
                            ct);
                        if (equipment is not null
                            && resourceKeys.Add($"LAB:{equipment.LabId}"))
                        {
                            await NotifyNextForResourceAsync(
                                equipment.LabId,
                                null,
                                booking.StartTime,
                                booking.EndTime,
                                ct);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }

        private async Task<Waitlist?> NotifyNextForResourceAsync(
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            var alreadyNotified = await _repository.ExistsAsync(
                x => x.Status == WaitlistStatus.Notified
                    && x.RequestedStart == requestedStart
                    && x.RequestedEnd == requestedEnd
                    && ((labId.HasValue && x.LabId == labId.Value)
                        || (equipmentId.HasValue && x.EquipmentId == equipmentId.Value)),
                cancellationToken);

            if (alreadyNotified)
                return null;

            var approvedConflict = await _unitOfWork.Bookings.HasBookingConflictAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                null,
                includePending: false,
                cancellationToken: cancellationToken);

            if (approvedConflict)
                return null;

            var maintenanceConflict = await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                null,
                cancellationToken);

            if (maintenanceConflict)
                return null;

            var next = await _repository.GetNextInQueueAsync(
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                cancellationToken);

            if (next is null)
                return null;

            next.MarkNotified();
            _repository.Update(next);

            var resourceName = labId.HasValue
                ? $"phòng lab ID {labId.Value}"
                : $"thiết bị ID {equipmentId!.Value}";

            await _unitOfWork.Notifications.AddAsync(
                new Notification(
                    next.UserId,
                    "Khung giờ trong hàng đợi đã có chỗ",
                    $"{resourceName} đã trống từ {requestedStart:dd/MM/yyyy HH:mm} "
                    + $"đến {requestedEnd:dd/MM/yyyy HH:mm}. Hãy tạo booking sớm.",
                    NotificationType.WaitlistAvailable),
                cancellationToken);

            return next;
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetRequiredUserId();
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");
            user.TryUnlockExpiredRestriction(DateTime.UtcNow);
            if (user.Status is UserStatus.Inactive or UserStatus.Locked)
                throw new InvalidOperationException("Tài khoản không được phép thao tác.");
            return user;
        }

        private async Task<User> GetCurrentActiveUserAsync(
            CancellationToken cancellationToken)
        {
            var user = await GetAuthenticatedUserAsync(cancellationToken);
            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản phải ở trạng thái Active.");
            return user;
        }

        private async Task EnsureCanCreateWaitlistAsync(
            User user,
            CancellationToken cancellationToken)
        {
            if (user.Status == UserStatus.Restricted)
            {
                if (user.RestrictionUntil.HasValue
                    && user.RestrictionUntil.Value <= DateTime.UtcNow)
                {
                    user.Unlock();
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return;
                }

                throw new InvalidOperationException(
                    "Tài khoản đang bị hạn chế nên không thể vào hàng đợi.");
            }
        }

        private static void EnsureManagerOrAdmin(User user)
        {
            if (user.Role?.RoleName is not RoleName.Admin and not RoleName.LabManager)
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được thực hiện thao tác này.");
        }

        private async Task ValidateResourceAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (labId.HasValue == equipmentId.HasValue)
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");

            if (labId.HasValue)
            {
                var lab = await _unitOfWork.LabRooms.GetByIdAsync(labId.Value, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {labId.Value}.");

                if (lab.Status == LabRoomStatus.Inactive)
                    throw new InvalidOperationException("Phòng lab đã ngừng hoạt động.");
            }
            else
            {
                var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                    equipmentId!.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");

                if (equipment.Status == EquipmentStatus.Retired)
                    throw new InvalidOperationException("Thiết bị đã ngừng sử dụng.");
            }
        }

        private async Task<bool> CanManageResourceAsync(
            int managerId,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            int resourceLabId;

            if (labId.HasValue)
            {
                resourceLabId = labId.Value;
            }
            else
            {
                var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                    equipmentId!.Value,
                    cancellationToken);

                if (equipment is null)
                    return false;

                resourceLabId = equipment.LabId;
            }

            var lab = await _unitOfWork.LabRooms.GetByIdAsync(
                resourceLabId,
                cancellationToken);

            return lab?.ManagerId == managerId;
        }

        private async Task<Waitlist> GetWaitlistOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy hàng đợi có ID {id}.");
        }

        private static void ValidateTime(
            DateTime requestedStart,
            DateTime requestedEnd,
            bool requireFuture)
        {
            if (requestedStart >= requestedEnd)
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (requireFuture && requestedStart <= DateTime.UtcNow)
                throw new ArgumentException("Thời gian bắt đầu phải ở tương lai.");
        }

        private static WaitlistResponse MapResponse(Waitlist waitlist)
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
