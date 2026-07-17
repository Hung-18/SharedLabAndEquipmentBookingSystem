using Application.DTOs;
using Application.DTOs.Booking;
using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class BookingService : IBookingService
    {
        private static readonly TimeSpan NoShowGracePeriod = TimeSpan.FromMinutes(30);
        private const int MaximumAlternativeSuggestions = 10;
        private const int MaximumAlternativeSearchDays = 30;
        private const int MinimumStepMinutes = 15;
        private const int MaximumStepMinutes = 240;

        private readonly IBookingRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWaitlistService _waitlistService;
        private readonly IViolationService _violationService;

        public BookingService(
            IBookingRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService,
            IWaitlistService waitlistService,
            IViolationService violationService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
            _waitlistService = waitlistService;
            _violationService = violationService;
        }

        public async Task<List<BookingResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            var bookings = await _repository.GetAllDetailedAsync(cancellationToken);
            bookings = await FilterForManagerAsync(actor, bookings, cancellationToken);

            return bookings.Select(MapResponse).ToList();
        }

        public async Task<BookingDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var booking = await _repository.GetDetailAsync(id, cancellationToken);

            if (booking is null)
                return null;

            await EnsureCanReadBookingAsync(actor, booking, cancellationToken);
            return MapDetailResponse(booking);
        }

        public async Task<List<BookingResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");

            if (actor.UserId != userId
                && actor.Role?.RoleName is not RoleName.Admin and not RoleName.LabManager)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chỉ được xem booking của chính mình.");
            }

            var bookings = await _repository.GetByUserIdAsync(userId, cancellationToken);
            bookings = await FilterForManagerAsync(actor, bookings, cancellationToken);

            return bookings.Select(MapResponse).ToList();
        }

        public async Task<List<BookingResponse>> GetPendingAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            var bookings = await _repository.GetPendingBookingsAsync(cancellationToken);
            bookings = await FilterForManagerAsync(actor, bookings, cancellationToken);

            return bookings.Select(MapResponse).ToList();
        }

        public async Task<List<CalendarEventResponse>> GetCalendarAsync(
            DateTime from,
            DateTime to,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);

            if (from >= to)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (labId.HasValue && equipmentId.HasValue)
                throw new ArgumentException("Chỉ được lọc theo LabId hoặc EquipmentId, không truyền cả hai.");

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                    equipmentId.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");

                equipmentLabId = equipment.LabId;
            }

            var bookings = await _repository.GetCalendarAsync(
                from,
                to,
                labId,
                equipmentId,
                cancellationToken);

            var maintenances = await _unitOfWork.Maintenances.GetActiveInRangeAsync(
                from,
                to,
                cancellationToken);

            if (labId.HasValue)
            {
                maintenances = maintenances
                    .Where(x =>
                        x.LabId == labId.Value
                        || x.Equipment?.LabId == labId.Value)
                    .ToList();
            }
            else if (equipmentId.HasValue)
            {
                maintenances = maintenances
                    .Where(x =>
                        x.EquipmentId == equipmentId.Value
                        || (equipmentLabId.HasValue
                            && x.LabId == equipmentLabId.Value))
                    .ToList();
            }

            var events = bookings
                .Select(MapBookingCalendarEvent)
                .Concat(maintenances.Select(MapMaintenanceCalendarEvent))
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EventType)
                .ThenBy(x => x.SourceId)
                .ToList();

            return events;
        }

        public async Task<List<SuggestedSlotResponse>> SuggestAlternativeSlotsAsync(
            AlternativeSlotRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);
            ValidateAlternativeSearchOptions(request);
            await GetAuthenticatedUserAsync(cancellationToken);

            var equipmentLabIds = await ValidateItemDefinitionsAsync(
                request.Items,
                cancellationToken);

            return await FindAlternativeSlotsCoreAsync(
                request.Items,
                request.StartTime,
                request.EndTime,
                request.MaxSuggestions,
                request.SearchDays,
                request.StepMinutes,
                equipmentLabIds,
                excludeBookingId: null,
                cancellationToken: cancellationToken);
        }

        public async Task<BookingDetailResponse> CreateAsync(
            CreateBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            var actor = await GetCurrentUserAsync(cancellationToken);
            await ValidateRequesterAsync(actor, cancellationToken);

            var priorityRule = await GetPriorityRuleAsync(
                request.PurposeType,
                cancellationToken);

            var equipmentLabIds = await ValidateItemDefinitionsAsync(
                request.Items,
                cancellationToken);

            await EnsureWaitlistHoldsAllowAsync(
                actor.UserId,
                request.Items,
                request.StartTime,
                request.EndTime,
                cancellationToken);

            if (await HasAnyConflictAsync(
                    request.Items,
                    request.StartTime,
                    request.EndTime,
                    excludeBookingId: null,
                    cancellationToken: cancellationToken))
            {
                var suggestedSlots = await FindAlternativeSlotsCoreAsync(
                    request.Items,
                    request.StartTime,
                    request.EndTime,
                    maxSuggestions: 3,
                    searchDays: 14,
                    stepMinutes: 30,
                    equipmentLabIds: equipmentLabIds,
                    excludeBookingId: null,
                    cancellationToken: cancellationToken);

                throw new ResourceUnavailableException(
                    "Tài nguyên không khả dụng trong khung giờ đã chọn.",
                    suggestedSlots);
            }

            var booking = new Booking(
                actor.UserId,
                priorityRule?.PriorityRuleId,
                request.PurposeType,
                request.PurposeDescription,
                request.StartTime,
                request.EndTime);

            foreach (var item in request.Items)
            {
                if (item.ResourceType == ResourceType.LabRoom)
                    booking.AddLabRoom(item.LabId!.Value, item.Note);
                else
                    booking.AddEquipment(item.EquipmentId!.Value, item.Note);
            }

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    // Re-check the waitlist hold and approved/maintenance conflicts
                    // inside the serializable transaction so another request cannot
                    // bypass a notified user's temporary reservation.
                    await EnsureWaitlistHoldsAllowAsync(
                        actor.UserId,
                        request.Items,
                        request.StartTime,
                        request.EndTime,
                        ct);

                    if (await HasAnyConflictAsync(
                            request.Items,
                            request.StartTime,
                            request.EndTime,
                            excludeBookingId: null,
                            cancellationToken: ct))
                    {
                        throw new ResourceUnavailableException(
                            "Tài nguyên vừa bị khóa bởi booking hoặc maintenance khác.",
                            Array.Empty<SuggestedSlotResponse>());
                    }

                    await _repository.AddAsync(booking, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    await MarkMatchingWaitlistHoldsBookedAsync(
                        actor.UserId,
                        request.Items,
                        request.StartTime,
                        request.EndTime,
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Create,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: null,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            var createdBooking = await _repository.GetDetailAsync(
                booking.BookingId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Không thể lấy thông tin booking vừa tạo.");

            return MapDetailResponse(createdBooking);
        }

        public async Task UpdateAsync(
            int id,
            UpdateBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            var actor = await GetCurrentUserAsync(cancellationToken);
            await ValidateRequesterAsync(actor, cancellationToken);
            var booking = await GetBookingOrThrowAsync(id, cancellationToken);

            if (booking.UserId != actor.UserId
                && actor.Role?.RoleName != RoleName.Admin)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ chủ booking hoặc Admin được sửa booking này.");
            }

            var priorityRule = await GetPriorityRuleAsync(
                request.PurposeType,
                cancellationToken);

            var currentItems = booking.BookingItems
                .Select(ToRequest)
                .ToList();

            var equipmentLabIds = await ValidateItemDefinitionsAsync(
                currentItems,
                cancellationToken);

            await EnsureWaitlistReservationUpdateAllowedAsync(
                actor.UserId,
                currentItems,
                booking.StartTime,
                booking.EndTime,
                request.StartTime,
                request.EndTime,
                cancellationToken);

            await EnsureWaitlistHoldsAllowAsync(
                actor.UserId,
                currentItems,
                request.StartTime,
                request.EndTime,
                cancellationToken);

            if (await HasAnyConflictAsync(
                    currentItems,
                    request.StartTime,
                    request.EndTime,
                    excludeBookingId: id,
                    cancellationToken: cancellationToken))
            {
                var suggestedSlots = await FindAlternativeSlotsCoreAsync(
                    currentItems,
                    request.StartTime,
                    request.EndTime,
                    maxSuggestions: 3,
                    searchDays: 14,
                    stepMinutes: 30,
                    equipmentLabIds: equipmentLabIds,
                    excludeBookingId: id,
                    cancellationToken: cancellationToken);

                throw new ResourceUnavailableException(
                    "Tài nguyên không khả dụng trong khung giờ đã chọn.",
                    suggestedSlots);
            }

            var oldValue = Snapshot(booking);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    await EnsureWaitlistHoldsAllowAsync(
                        actor.UserId,
                        currentItems,
                        request.StartTime,
                        request.EndTime,
                        ct);

                    if (await HasAnyConflictAsync(
                            currentItems,
                            request.StartTime,
                            request.EndTime,
                            excludeBookingId: id,
                            cancellationToken: ct))
                    {
                        throw new ResourceUnavailableException(
                            "Tài nguyên vừa bị khóa bởi booking hoặc maintenance khác.",
                            Array.Empty<SuggestedSlotResponse>());
                    }

                    booking.UpdateDetails(
                        priorityRule?.PriorityRuleId,
                        request.PurposeType,
                        request.PurposeDescription,
                        request.StartTime,
                        request.EndTime);

                    _repository.Update(booking);

                    await MarkMatchingWaitlistHoldsBookedAsync(
                        actor.UserId,
                        currentItems,
                        request.StartTime,
                        request.EndTime,
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task ApproveAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var booking = await GetBookingOrThrowAsync(id, ct);
                    if (booking.StartTime <= DateTime.UtcNow)
                    {
                        throw new InvalidOperationException(
                            "Không thể duyệt booking đã đến hoặc đã qua giờ bắt đầu.");
                    }

                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    var items = booking.BookingItems
                        .Select(ToRequest)
                        .ToList();

                    await EnsureWaitlistHoldsAllowAsync(
                        booking.UserId,
                        items,
                        booking.StartTime,
                        booking.EndTime,
                        ct);

                    bool hasWaitlistReservation =
                        await HasMatchingWaitlistReservationAsync(
                            booking.UserId,
                            items,
                            booking.StartTime,
                            booking.EndTime,
                            ct);

                    if (!hasWaitlistReservation)
                        await EnsurePriorityTurnAsync(booking, ct);

                    await ValidateItemsAsync(
                        items,
                        booking.StartTime,
                        booking.EndTime,
                        excludeBookingId: id,
                        ct);

                    var oldValue = Snapshot(booking);
                    booking.Approve(actor.UserId);
                    _repository.Update(booking);

                    await MarkMatchingWaitlistHoldsBookedAsync(
                        booking.UserId,
                        items,
                        booking.StartTime,
                        booking.EndTime,
                        ct);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            booking.UserId,
                            "Booking đã được duyệt",
                            $"Booking #{booking.BookingId} từ {booking.StartTime:dd/MM/yyyy HH:mm} "
                            + $"đến {booking.EndTime:dd/MM/yyyy HH:mm} đã được duyệt.",
                            NotificationType.BookingApproved),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.ApproveBooking,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task RejectAsync(
            int id,
            RejectBookingRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var booking = await GetBookingOrThrowAsync(id, ct);
                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    var oldValue = Snapshot(booking);
                    booking.Reject(actor.UserId, request.RejectionReason);
                    _repository.Update(booking);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            booking.UserId,
                            "Booking bị từ chối",
                            $"Booking #{booking.BookingId} bị từ chối. Lý do: {booking.RejectionReason}",
                            NotificationType.BookingRejected),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.RejectBooking,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);

                    await _waitlistService.NotifyNextForReleasedBookingAsync(
                        booking.BookingId,
                        ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var booking = await GetBookingOrThrowAsync(id, ct);

                    var isOwner = booking.UserId == actor.UserId;
                    var isAdmin = actor.Role?.RoleName == RoleName.Admin;
                    var isManagingManager =
                        actor.Role?.RoleName == RoleName.LabManager
                        && await CanManageBookingAsync(actor.UserId, booking, ct);

                    if (!isOwner && !isAdmin && !isManagingManager)
                    {
                        throw new UnauthorizedAccessException(
                            "Bạn không có quyền hủy booking này.");
                    }

                    if (booking.Status == BookingStatus.Approved
                        && DateTime.UtcNow >= booking.StartTime)
                    {
                        throw new InvalidOperationException(
                            "Không thể hủy booking Approved sau khi đã đến giờ bắt đầu.");
                    }

                    var usageLogs = await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                        booking.BookingId,
                        ct);

                    if (usageLogs.Any(x => x.ActualCheckout is null))
                    {
                        throw new InvalidOperationException(
                            "Không thể hủy booking khi đang có tài nguyên chưa checkout.");
                    }

                    bool shouldReleaseSlot = booking.Status
                        is BookingStatus.Pending or BookingStatus.Approved;
                    var oldValue = Snapshot(booking);

                    booking.Cancel();
                    _repository.Update(booking);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            booking.UserId,
                            "Booking đã bị hủy",
                            $"Booking #{booking.BookingId} đã bị hủy.",
                            NotificationType.System),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);

                    if (shouldReleaseSlot)
                    {
                        await _waitlistService.NotifyNextForCancelledBookingAsync(
                            booking.BookingId,
                            ct);
                    }

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task CompleteAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var booking = await GetBookingOrThrowAsync(id, ct);
                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    var now = DateTime.UtcNow;
                    if (now < booking.StartTime)
                    {
                        throw new InvalidOperationException(
                            "Không thể hoàn thành booking trước giờ bắt đầu.");
                    }

                    var allItemsCheckedOut = await AreAllItemsCheckedOutAsync(
                        booking,
                        ct);

                    if (now < booking.EndTime && !allItemsCheckedOut)
                    {
                        throw new InvalidOperationException(
                            "Chỉ được hoàn thành trước giờ kết thúc khi tất cả tài nguyên đã checkout.");
                    }

                    var oldValue = Snapshot(booking);
                    booking.Complete();
                    _repository.Update(booking);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            booking.UserId,
                            "Booking đã hoàn thành",
                            $"Booking #{booking.BookingId} đã được hoàn thành.",
                            NotificationType.System),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);

                    await _waitlistService.NotifyNextForReleasedBookingAsync(
                        booking.BookingId,
                        ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task MarkNoShowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var booking = await GetBookingOrThrowAsync(id, ct);
                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    if (DateTime.UtcNow < booking.StartTime.Add(NoShowGracePeriod))
                    {
                        throw new InvalidOperationException(
                            $"Chỉ được đánh dấu NoShow sau {NoShowGracePeriod.TotalMinutes:0} phút kể từ giờ bắt đầu.");
                    }

                    var usageLogs = await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                        booking.BookingId,
                        ct);

                    if (usageLogs.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "Booking đã có check-in nên không thể đánh dấu NoShow.");
                    }

                    var oldValue = Snapshot(booking);
                    booking.MarkNoShow();
                    _repository.Update(booking);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            booking.UserId,
                            "Booking bị đánh dấu NoShow",
                            $"Booking #{booking.BookingId} đã bị đánh dấu NoShow.",
                            NotificationType.Violation),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Booking),
                        entityId: booking.BookingId,
                        oldValue: oldValue,
                        newValue: Snapshot(booking),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);

                    await _violationService.EnsureAutomaticViolationAsync(
                        booking.BookingId,
                        ViolationType.NoShow,
                        ct);

                    await _waitlistService.NotifyNextForReleasedBookingAsync(
                        booking.BookingId,
                        ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        private async Task<User> GetCurrentUserAsync(
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetRequiredUserId();
            return await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            var user = await GetCurrentUserAsync(cancellationToken);
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

        private async Task ValidateRequesterAsync(
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
                    user.RestrictionUntil.HasValue
                        ? $"Người dùng bị hạn chế đặt lịch đến {user.RestrictionUntil:yyyy-MM-dd HH:mm:ss} UTC."
                        : "Người dùng đang bị hạn chế đặt lịch.");
            }

            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản không ở trạng thái hoạt động.");
        }

        private static void EnsureManagerOrAdmin(User user)
        {
            if (user.Role?.RoleName is not RoleName.Admin and not RoleName.LabManager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được thực hiện thao tác này.");
            }
        }

        private async Task EnsureCanReadBookingAsync(
            User actor,
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (actor.UserId == booking.UserId || actor.Role?.RoleName == RoleName.Admin)
                return;

            if (actor.Role?.RoleName == RoleName.LabManager
                && await CanManageBookingAsync(actor.UserId, booking, cancellationToken))
            {
                return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền xem booking này.");
        }

        private async Task EnsureManagerCanManageBookingAsync(
            User actor,
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (actor.Role?.RoleName == RoleName.Admin)
                return;

            if (actor.Role?.RoleName != RoleName.LabManager
                || !await CanManageBookingAsync(actor.UserId, booking, cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được thao tác với tài nguyên thuộc phòng mình quản lý.");
            }
        }

        private async Task<bool> CanManageBookingAsync(
            int managerId,
            Booking booking,
            CancellationToken cancellationToken)
        {
            var managedLabs = await _unitOfWork.LabRooms.GetByManagerIdAsync(
                managerId,
                cancellationToken);

            var managedLabIds = managedLabs.Select(x => x.LabId).ToHashSet();
            return BookingBelongsToManagedLabs(booking, managedLabIds);
        }

        private async Task<IReadOnlyList<Booking>> FilterForManagerAsync(
            User actor,
            IReadOnlyList<Booking> bookings,
            CancellationToken cancellationToken)
        {
            if (actor.Role?.RoleName != RoleName.LabManager)
                return bookings;

            var managedLabs = await _unitOfWork.LabRooms.GetByManagerIdAsync(
                actor.UserId,
                cancellationToken);

            var managedLabIds = managedLabs.Select(x => x.LabId).ToHashSet();
            return bookings
                .Where(x => BookingBelongsToManagedLabs(x, managedLabIds))
                .ToList();
        }

        private static bool BookingBelongsToManagedLabs(
            Booking booking,
            HashSet<int> managedLabIds)
        {
            return booking.BookingItems.Count > 0
                && booking.BookingItems.All(item =>
                {
                    var labId = item.LabId ?? item.Equipment?.LabId;
                    return labId.HasValue && managedLabIds.Contains(labId.Value);
                });
        }

        private async Task EnsurePriorityTurnAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            var competitors = await _repository.GetCompetingPendingBookingsAsync(
                booking.BookingId,
                booking.StartTime,
                booking.EndTime,
                cancellationToken);

            var firstInQueue = competitors
                .Append(booking)
                .OrderBy(GetEffectivePriority)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.BookingId)
                .First();

            if (firstInQueue.BookingId != booking.BookingId)
            {
                throw new InvalidOperationException(
                    $"Booking #{firstInQueue.BookingId} có mức ưu tiên cao hơn hoặc được tạo sớm hơn và phải được xử lý trước.");
            }
        }

        private static int GetEffectivePriority(Booking booking)
        {
            return booking.PriorityRule?.PriorityLevel ?? int.MaxValue;
        }

        private async Task<PriorityRule?> GetPriorityRuleAsync(
            BookingPurposeType purposeType,
            CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(purposeType))
                throw new ArgumentException("Loại mục đích đặt lịch không hợp lệ.");

            var rule = await _unitOfWork.PriorityRules.GetByPurposeTypeAsync(
                purposeType,
                cancellationToken);

            return rule?.Status == PriorityRuleStatus.Active ? rule : null;
        }

        private async Task ValidateItemsAsync(
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            await ValidateItemDefinitionsAsync(items, cancellationToken);

            foreach (var item in items)
            {
                await EnsureNoConflictAsync(
                    item.LabId,
                    item.EquipmentId,
                    startTime,
                    endTime,
                    excludeBookingId,
                    cancellationToken);
            }
        }

        private async Task<Dictionary<int, int>> ValidateItemDefinitionsAsync(
            IReadOnlyCollection<BookingItemRequest> items,
            CancellationToken cancellationToken)
        {
            if (items is null || items.Count == 0)
            {
                throw new ArgumentException(
                    "Booking phải có ít nhất một phòng lab hoặc thiết bị.");
            }

            var resourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resourceLabIds = new HashSet<int>();
            var equipmentLabIds = new Dictionary<int, int>();

            foreach (var item in items)
            {
                if (!Enum.IsDefined(item.ResourceType))
                    throw new ArgumentException("Loại tài nguyên không hợp lệ.");

                if (item.ResourceType == ResourceType.LabRoom)
                {
                    if (!item.LabId.HasValue
                        || item.LabId.Value <= 0
                        || item.EquipmentId.HasValue)
                    {
                        throw new ArgumentException(
                            "Item phòng lab phải có LabId hợp lệ và EquipmentId phải null.");
                    }

                    if (!resourceKeys.Add($"LAB:{item.LabId.Value}"))
                    {
                        throw new ArgumentException(
                            $"Phòng lab ID {item.LabId.Value} bị lặp trong booking.");
                    }

                    var lab = await _unitOfWork.LabRooms.GetByIdAsync(
                        item.LabId.Value,
                        cancellationToken)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy phòng lab có ID {item.LabId.Value}.");

                    if (lab.Status is LabRoomStatus.Inactive or LabRoomStatus.Unavailable)
                    {
                        throw new InvalidOperationException(
                            $"Phòng lab ID {item.LabId.Value} hiện không thể đặt lịch.");
                    }

                    resourceLabIds.Add(lab.LabId);
                }
                else
                {
                    if (!item.EquipmentId.HasValue
                        || item.EquipmentId.Value <= 0
                        || item.LabId.HasValue)
                    {
                        throw new ArgumentException(
                            "Item thiết bị phải có EquipmentId hợp lệ và LabId phải null.");
                    }

                    if (!resourceKeys.Add($"EQUIPMENT:{item.EquipmentId.Value}"))
                    {
                        throw new ArgumentException(
                            $"Thiết bị ID {item.EquipmentId.Value} bị lặp trong booking.");
                    }

                    var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                        item.EquipmentId.Value,
                        cancellationToken)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy thiết bị có ID {item.EquipmentId.Value}.");

                    if (equipment.Status is EquipmentStatus.Broken or EquipmentStatus.Retired)
                    {
                        throw new InvalidOperationException(
                            $"Thiết bị ID {item.EquipmentId.Value} hiện không thể đặt lịch.");
                    }

                    if (equipment.LabRoom is not null
                        && equipment.LabRoom.Status is LabRoomStatus.Inactive or LabRoomStatus.Unavailable)
                    {
                        throw new InvalidOperationException(
                            "Phòng chứa thiết bị hiện không thể đặt lịch.");
                    }

                    equipmentLabIds[equipment.EquipmentId] = equipment.LabId;
                    resourceLabIds.Add(equipment.LabId);
                }
            }

            if (resourceLabIds.Count > 1)
            {
                throw new InvalidOperationException(
                    "Một booking chỉ được chứa tài nguyên thuộc cùng một phòng lab để bảo đảm đúng phạm vi duyệt của LabManager.");
            }

            return equipmentLabIds;
        }

        private async Task<bool> HasAnyConflictAsync(
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                bool bookingConflict = await _repository.HasBookingConflictAsync(
                    item.LabId,
                    item.EquipmentId,
                    startTime,
                    endTime,
                    excludeBookingId,
                    includePending: false,
                    cancellationToken: cancellationToken);

                if (bookingConflict)
                    return true;

                bool maintenanceConflict =
                    await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                        item.LabId,
                        item.EquipmentId,
                        startTime,
                        endTime,
                        excludeMaintenanceId: null,
                        cancellationToken: cancellationToken);

                if (maintenanceConflict)
                    return true;
            }

            return false;
        }

        private async Task<List<SuggestedSlotResponse>> FindAlternativeSlotsCoreAsync(
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime requestedStart,
            DateTime requestedEnd,
            int maxSuggestions,
            int searchDays,
            int stepMinutes,
            IReadOnlyDictionary<int, int> equipmentLabIds,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            TimeSpan duration = requestedEnd - requestedStart;
            TimeSpan step = TimeSpan.FromMinutes(stepMinutes);
            DateTime earliest = requestedStart.Add(step);
            DateTime serverEarliest = DateTime.UtcNow.Add(step);

            if (earliest < serverEarliest)
                earliest = serverEarliest;

            DateTime candidateStart = RoundUp(earliest, stepMinutes);
            DateTime searchEnd = requestedStart.AddDays(searchDays);

            if (searchEnd <= candidateStart)
                searchEnd = candidateStart.AddDays(searchDays);

            var bookings = await _repository.GetCalendarAsync(
                candidateStart,
                searchEnd.Add(duration),
                null,
                null,
                cancellationToken);

            var blockingBookings = bookings
                .Where(x => x.Status == BookingStatus.Approved)
                .Where(x => !excludeBookingId.HasValue
                    || x.BookingId != excludeBookingId.Value)
                .ToList();

            var maintenances = await _unitOfWork.Maintenances.GetActiveInRangeAsync(
                candidateStart,
                searchEnd.Add(duration),
                cancellationToken);

            var results = new List<SuggestedSlotResponse>();

            while (candidateStart.Add(duration) <= searchEnd
                && results.Count < maxSuggestions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime candidateEnd = candidateStart.Add(duration);

                bool available = items.All(item =>
                    !HasBookingConflictInMemory(
                        item,
                        candidateStart,
                        candidateEnd,
                        blockingBookings,
                        equipmentLabIds)
                    && !HasMaintenanceConflictInMemory(
                        item,
                        candidateStart,
                        candidateEnd,
                        maintenances,
                        equipmentLabIds));

                if (available)
                {
                    results.Add(new SuggestedSlotResponse
                    {
                        StartTime = candidateStart,
                        EndTime = candidateEnd
                    });

                    candidateStart = RoundUp(candidateEnd, stepMinutes);
                }
                else
                {
                    candidateStart = candidateStart.Add(step);
                }
            }

            return results;
        }

        private static bool HasBookingConflictInMemory(
            BookingItemRequest requestedItem,
            DateTime startTime,
            DateTime endTime,
            IReadOnlyCollection<Booking> bookings,
            IReadOnlyDictionary<int, int> equipmentLabIds)
        {
            return bookings.Any(booking =>
                booking.StartTime < endTime
                && booking.EndTime > startTime
                && booking.BookingItems.Any(existingItem =>
                {
                    if (requestedItem.ResourceType == ResourceType.LabRoom)
                    {
                        int labId = requestedItem.LabId!.Value;
                        return existingItem.LabId == labId
                            || existingItem.Equipment?.LabId == labId;
                    }

                    int equipmentId = requestedItem.EquipmentId!.Value;
                    int equipmentLabId = equipmentLabIds[equipmentId];
                    return existingItem.EquipmentId == equipmentId
                        || existingItem.LabId == equipmentLabId;
                }));
        }

        private static bool HasMaintenanceConflictInMemory(
            BookingItemRequest requestedItem,
            DateTime startTime,
            DateTime endTime,
            IReadOnlyCollection<Maintenance> maintenances,
            IReadOnlyDictionary<int, int> equipmentLabIds)
        {
            return maintenances.Any(maintenance =>
            {
                if (maintenance.Status is not MaintenanceStatus.Scheduled
                    and not MaintenanceStatus.InProgress)
                {
                    return false;
                }

                if (maintenance.StartTime >= endTime
                    || maintenance.EndTime <= startTime)
                {
                    return false;
                }

                if (requestedItem.ResourceType == ResourceType.LabRoom)
                {
                    int labId = requestedItem.LabId!.Value;
                    return maintenance.LabId == labId;
                }

                int equipmentId = requestedItem.EquipmentId!.Value;
                int equipmentLabId = equipmentLabIds[equipmentId];
                return maintenance.EquipmentId == equipmentId
                    || maintenance.LabId == equipmentLabId;
            });
        }

        private static DateTime RoundUp(DateTime value, int stepMinutes)
        {
            long stepTicks = TimeSpan.FromMinutes(stepMinutes).Ticks;
            long roundedTicks = ((value.Ticks + stepTicks - 1) / stepTicks) * stepTicks;
            return new DateTime(roundedTicks, value.Kind);
        }

        private static void ValidateAlternativeSearchOptions(
            AlternativeSlotRequest request)
        {
            if (request.MaxSuggestions <= 0
                || request.MaxSuggestions > MaximumAlternativeSuggestions)
            {
                throw new ArgumentException(
                    $"MaxSuggestions phải nằm trong khoảng 1 đến {MaximumAlternativeSuggestions}.");
            }

            if (request.SearchDays <= 0
                || request.SearchDays > MaximumAlternativeSearchDays)
            {
                throw new ArgumentException(
                    $"SearchDays phải nằm trong khoảng 1 đến {MaximumAlternativeSearchDays}.");
            }

            if (request.StepMinutes < MinimumStepMinutes
                || request.StepMinutes > MaximumStepMinutes)
            {
                throw new ArgumentException(
                    $"StepMinutes phải nằm trong khoảng {MinimumStepMinutes} đến {MaximumStepMinutes}.");
            }
        }

        private async Task<bool> HasMatchingWaitlistReservationAsync(
            int userId,
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            if (items.Count != 1)
                return false;

            var item = items.Single();
            var reservation =
                await _unitOfWork.Waitlists.GetActiveReservationAsync(
                    item.LabId,
                    item.EquipmentId,
                    startTime,
                    endTime,
                    cancellationToken);

            if (reservation is null
                || reservation.UserId != userId
                || reservation.RequestedStart != startTime
                || reservation.RequestedEnd != endTime)
            {
                return false;
            }

            return (reservation.LabId.HasValue
                    && item.LabId == reservation.LabId
                    && item.EquipmentId is null)
                || (reservation.EquipmentId.HasValue
                    && item.EquipmentId == reservation.EquipmentId
                    && item.LabId is null);
        }

        private async Task EnsureWaitlistReservationUpdateAllowedAsync(
            int userId,
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime currentStart,
            DateTime currentEnd,
            DateTime requestedStart,
            DateTime requestedEnd,
            CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var reservation =
                    await _unitOfWork.Waitlists.GetActiveReservationAsync(
                        item.LabId,
                        item.EquipmentId,
                        currentStart,
                        currentEnd,
                        cancellationToken);

                if (reservation is null
                    || reservation.Status != WaitlistStatus.Booked
                    || reservation.UserId != userId
                    || reservation.RequestedStart != currentStart
                    || reservation.RequestedEnd != currentEnd)
                {
                    continue;
                }

                if (requestedStart != currentStart
                    || requestedEnd != currentEnd)
                {
                    throw new InvalidOperationException(
                        "Booking được tạo từ waitlist phải giữ nguyên khung giờ. "
                        + "Hãy hủy booking để chuyển quyền ưu tiên cho người tiếp theo.");
                }
            }
        }

        private async Task EnsureWaitlistHoldsAllowAsync(
            int userId,
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            var holds = new Dictionary<int, Waitlist>();

            foreach (var item in items)
            {
                var hold = await _unitOfWork.Waitlists.GetActiveReservationAsync(
                    item.LabId,
                    item.EquipmentId,
                    startTime,
                    endTime,
                    cancellationToken);

                if (hold is not null)
                    holds[hold.WaitlistId] = hold;
            }

            if (holds.Count == 0)
                return;

            if (holds.Values.Any(x => x.UserId != userId))
            {
                throw new InvalidOperationException(
                    "Khung giờ đang được giữ ưu tiên cho người dùng khác trong waitlist.");
            }

            if (items.Count != 1 || holds.Count != 1)
            {
                throw new InvalidOperationException(
                    "Khi nhận chỗ từ waitlist, booking phải chứa đúng tài nguyên đã được thông báo.");
            }

            var requestedItem = items.Single();
            var activeHold = holds.Values.Single();

            bool exactResource =
                (activeHold.LabId.HasValue
                    && requestedItem.LabId == activeHold.LabId
                    && requestedItem.EquipmentId is null)
                || (activeHold.EquipmentId.HasValue
                    && requestedItem.EquipmentId == activeHold.EquipmentId
                    && requestedItem.LabId is null);

            if (!exactResource
                || activeHold.RequestedStart != startTime
                || activeHold.RequestedEnd != endTime)
            {
                throw new InvalidOperationException(
                    "Booking phải dùng đúng tài nguyên và đúng khung giờ đã được waitlist giữ chỗ.");
            }
        }

        private async Task MarkMatchingWaitlistHoldsBookedAsync(
            int userId,
            IReadOnlyCollection<BookingItemRequest> items,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var hold = await _unitOfWork.Waitlists.GetActiveReservationAsync(
                    item.LabId,
                    item.EquipmentId,
                    startTime,
                    endTime,
                    cancellationToken);

                if (hold is null
                    || hold.UserId != userId
                    || hold.RequestedStart != startTime
                    || hold.RequestedEnd != endTime)
                {
                    continue;
                }

                bool exactResource =
                    (hold.LabId.HasValue
                        && item.LabId == hold.LabId
                        && item.EquipmentId is null)
                    || (hold.EquipmentId.HasValue
                        && item.EquipmentId == hold.EquipmentId
                        && item.LabId is null);

                if (!exactResource)
                    continue;

                if (hold.Status == WaitlistStatus.Notified)
                {
                    hold.MarkBooked();
                    _unitOfWork.Waitlists.Update(hold);
                }
            }
        }

        private async Task EnsureNoConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeBookingId,
            CancellationToken cancellationToken)
        {
            var bookingConflict = await _repository.HasBookingConflictAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeBookingId,
                includePending: false,
                cancellationToken: cancellationToken);

            if (bookingConflict)
                throw new InvalidOperationException("Khung giờ này đã có booking Approved khác.");

            var maintenanceConflict = await _unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeMaintenanceId: null,
                cancellationToken: cancellationToken);

            if (maintenanceConflict)
                throw new InvalidOperationException("Khung giờ này đang có lịch bảo trì.");
        }

        private async Task<Booking> GetBookingOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _repository.GetDetailAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy booking có ID {id}.");
        }

        private async Task<bool> AreAllItemsCheckedOutAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (booking.BookingItems.Count == 0)
                return false;

            var logs = await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                booking.BookingId,
                cancellationToken);

            return booking.BookingItems.All(item =>
                logs.Any(log =>
                    log.BookingItemId == item.BookingItemId
                    && log.ActualCheckout.HasValue));
        }

        private static void ValidateTime(
            DateTime startTime,
            DateTime endTime,
            bool requireFuture)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (requireFuture && startTime <= DateTime.UtcNow)
                throw new ArgumentException("Thời gian bắt đầu phải ở tương lai.");
        }

        private static BookingItemRequest ToRequest(BookingItem item)
        {
            return new BookingItemRequest
            {
                ResourceType = item.ResourceType,
                LabId = item.LabId,
                EquipmentId = item.EquipmentId,
                Note = item.Note
            };
        }

        private static object Snapshot(Booking booking)
        {
            return new
            {
                booking.BookingId,
                booking.UserId,
                booking.PriorityRuleId,
                PriorityLevel = booking.PriorityRule?.PriorityLevel,
                PurposeType = booking.PurposeType.ToString(),
                booking.PurposeDescription,
                booking.StartTime,
                booking.EndTime,
                Status = booking.Status.ToString(),
                booking.ApprovedById,
                booking.ApprovedAt,
                booking.RejectionReason
            };
        }

        private static CalendarEventResponse MapBookingCalendarEvent(
            Booking booking)
        {
            return new CalendarEventResponse
            {
                EventType = "Booking",
                SourceId = booking.BookingId,
                Title = $"Booking #{booking.BookingId} - {booking.PurposeType}",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                Blocking = booking.Status == BookingStatus.Approved,
                UserId = booking.UserId,
                Resources = booking.BookingItems
                    .Select(item => new CalendarResourceResponse
                    {
                        ResourceType = item.ResourceType.ToString(),
                        ResourceId = item.LabId ?? item.EquipmentId!.Value,
                        LabId = item.LabId
                            ?? item.Equipment?.LabId
                            ?? 0,
                        ResourceName = item.LabRoom?.LabName
                            ?? item.Equipment?.EquipmentName
                            ?? string.Empty
                    })
                    .ToList()
            };
        }

        private static CalendarEventResponse MapMaintenanceCalendarEvent(
            Maintenance maintenance)
        {
            bool isLab = maintenance.LabId.HasValue;

            return new CalendarEventResponse
            {
                EventType = "Maintenance",
                SourceId = maintenance.MaintenanceId,
                Title = isLab
                    ? $"Bảo trì phòng {maintenance.LabRoom?.LabName ?? maintenance.LabId!.Value.ToString()}"
                    : $"Bảo trì thiết bị {maintenance.Equipment?.EquipmentName ?? maintenance.EquipmentId!.Value.ToString()}",
                StartTime = maintenance.StartTime,
                EndTime = maintenance.EndTime,
                Status = maintenance.Status.ToString(),
                Blocking = maintenance.Status is MaintenanceStatus.Scheduled
                    or MaintenanceStatus.InProgress,
                UserId = null,
                Resources = new List<CalendarResourceResponse>
                {
                    new()
                    {
                        ResourceType = isLab
                            ? ResourceType.LabRoom.ToString()
                            : ResourceType.Equipment.ToString(),
                        ResourceId = maintenance.LabId
                            ?? maintenance.EquipmentId!.Value,
                        LabId = maintenance.LabId
                            ?? maintenance.Equipment?.LabId
                            ?? 0,
                        ResourceName = isLab
                            ? maintenance.LabRoom?.LabName ?? string.Empty
                            : maintenance.Equipment?.EquipmentName ?? string.Empty
                    }
                }
            };
        }

        private static BookingResponse MapResponse(Booking booking)
        {
            return new BookingResponse
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                PriorityRuleId = booking.PriorityRuleId,
                PriorityLevel = booking.PriorityRule?.PriorityLevel,
                PurposeType = booking.PurposeType.ToString(),
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        private static BookingDetailResponse MapDetailResponse(Booking booking)
        {
            return new BookingDetailResponse
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                UserName = booking.User?.FullName,
                PriorityRuleId = booking.PriorityRuleId,
                PriorityLevel = booking.PriorityRule?.PriorityLevel,
                ApprovedById = booking.ApprovedById,
                ApprovedByName = booking.ApprovedBy?.FullName,
                PurposeType = booking.PurposeType.ToString(),
                PurposeDescription = booking.PurposeDescription,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                RejectionReason = booking.RejectionReason,
                ApprovedAt = booking.ApprovedAt,
                CreatedAt = booking.CreatedAt,
                Items = booking.BookingItems.Select(item => new BookingItemResponse
                {
                    BookingItemId = item.BookingItemId,
                    ResourceType = item.ResourceType.ToString(),
                    LabId = item.LabId,
                    LabName = item.LabRoom?.LabName,
                    EquipmentId = item.EquipmentId,
                    EquipmentName = item.Equipment?.EquipmentName,
                    Note = item.Note
                }).ToList()
            };
        }

        //public async Task<PageResult<BookingResponse>> PageResultAsync(int page, int pageSize, CancellationToken cancelation)
        //{
        //    var user = _currentUserService.UserId;
        //    if (user == null)
        //    {
        //        throw new UnauthorizedAccessException("Please login to continue!");
        //    }
        //    var booking = await _repository.PageResultAsync(user.Value, page, pageSize, cancelation);
        //    var total = await _repository.CountPageAsync(user);
        //    var data = booking.Select(b => new BookingResponse
        //    {
        //        BookingId = b.BookingId,
        //        UserId = b.UserId,
        //        PurposeType = b.PurposeType.ToString() ?? "N/A",
        //        StartTime = b.StartTime,
        //        EndTime = b.EndTime,
        //        Status = b.Status.ToString(), 
        //        CreatedAt = b.CreatedAt
        //    }).ToList();
        //    return new PageResult<BookingResponse>
        //    {
        //        Page = page,
        //        PageSize = pageSize,
        //        Total = total,
        //        Data = data
        //    };
        //}
    }
}
