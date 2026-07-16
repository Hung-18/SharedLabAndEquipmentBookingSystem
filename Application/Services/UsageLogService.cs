using Application.DTOs.UsageLogs;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class UsageLogService : IUsageLogService
    {
        private static readonly TimeSpan CheckInEarlyWindow =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan CheckInLateWindow =
            TimeSpan.FromMinutes(30);

        private static readonly TimeSpan FutureClockTolerance =
            TimeSpan.FromMinutes(1);

        private readonly IUsageLogRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;
        private readonly IViolationService _violationService;
        private readonly IWaitlistService _waitlistService;

        public UsageLogService(
            IUsageLogRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService,
            IViolationService violationService,
            IWaitlistService waitlistService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
            _violationService = violationService;
            _waitlistService = waitlistService;
        }

        public async Task<List<UsageLogResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            var logs = await _repository.GetAllAsync(cancellationToken);

            if (actor.Role?.RoleName == RoleName.LabManager)
            {
                var managedLabIds = await GetManagedLabIdsAsync(
                    actor.UserId,
                    cancellationToken);
                var permittedLogs = new List<UsageLog>();

                foreach (var log in logs)
                {
                    var booking = await GetBookingForLogAsync(
                        log,
                        cancellationToken);

                    if (BookingBelongsToManagedLabs(booking, managedLabIds))
                        permittedLogs.Add(log);
                }

                logs = permittedLogs;
            }

            return logs.Select(MapResponse).ToList();
        }

        public async Task<UsageLogResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var log = await _repository.GetByIdAsync(id, cancellationToken);

            if (log is null)
                return null;

            var booking = await GetBookingForLogAsync(log, cancellationToken);
            await EnsureCanAccessBookingAsync(actor, booking, cancellationToken);

            return MapResponse(log);
        }

        public async Task<List<UsageLogResponse>> GetByBookingItemIdAsync(
            int bookingItemId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var bookingItem = await _unitOfWork.BookingItems.GetByIdAsync(
                bookingItemId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy BookingItem có ID {bookingItemId}.");

            var booking = await _unitOfWork.Bookings.GetDetailAsync(
                bookingItem.BookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingItem.BookingId}.");

            await EnsureCanAccessBookingAsync(actor, booking, cancellationToken);

            var logs = await _repository.GetByBookingItemIdAsync(
                bookingItemId,
                cancellationToken);

            return logs.Select(MapResponse).ToList();
        }

        public async Task<List<UsageLogResponse>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var booking = await _unitOfWork.Bookings.GetDetailAsync(
                bookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");

            await EnsureCanAccessBookingAsync(actor, booking, cancellationToken);

            var logs = await _repository.GetByBookingIdAsync(
                bookingId,
                cancellationToken);

            return logs.Select(MapResponse).ToList();
        }

        public async Task<List<UsageLogResponse>> GetIncidentLogsAsync(
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            var logs = await _repository.GetIncidentLogsAsync(
                from,
                to,
                cancellationToken);

            if (actor.Role?.RoleName == RoleName.LabManager)
            {
                var managedLabIds = await GetManagedLabIdsAsync(
                    actor.UserId,
                    cancellationToken);

                var permittedLogs = new List<UsageLog>();
                foreach (var log in logs)
                {
                    var booking = await GetBookingForLogAsync(
                        log,
                        cancellationToken);

                    if (BookingBelongsToManagedLabs(booking, managedLabIds))
                        permittedLogs.Add(log);
                }

                logs = permittedLogs;
            }

            return logs.Select(MapResponse).ToList();
        }

        public async Task<UsageLogResponse> CheckInAsync(
            CheckInUsageRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            UsageLog? createdLog = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var actor = await GetAuthenticatedUserAsync(ct);
                    var bookingItem = await _unitOfWork.BookingItems.GetByIdAsync(
                        request.BookingItemId,
                        ct)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy BookingItem có ID {request.BookingItemId}.");

                    var booking = await _unitOfWork.Bookings.GetDetailAsync(
                        bookingItem.BookingId,
                        ct)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy booking có ID {bookingItem.BookingId}.");

                    await EnsureCanAccessBookingAsync(actor, booking, ct);

                    if (booking.Status != BookingStatus.Approved)
                    {
                        throw new InvalidOperationException(
                            "Chỉ booking đã được duyệt mới được check-in.");
                    }

                    DateTime actualCheckin = ResolveActualTime(
                        request.ActualCheckin,
                        actor,
                        "check-in");

                    DateTime earliestCheckIn =
                        booking.StartTime.Subtract(CheckInEarlyWindow);
                    DateTime latestCheckIn =
                        booking.StartTime.Add(CheckInLateWindow) < booking.EndTime
                            ? booking.StartTime.Add(CheckInLateWindow)
                            : booking.EndTime;

                    if (actualCheckin < earliestCheckIn
                        || actualCheckin > latestCheckIn)
                    {
                        throw new InvalidOperationException(
                            $"Chỉ được check-in từ {earliestCheckIn:yyyy-MM-dd HH:mm:ss} "
                            + $"đến {latestCheckIn:yyyy-MM-dd HH:mm:ss} UTC.");
                    }

                    if (await _repository.HasOpenLogAsync(
                            request.BookingItemId,
                            ct))
                    {
                        throw new InvalidOperationException(
                            "BookingItem này đang có một lượt sử dụng chưa checkout.");
                    }

                    await MarkResourceInUseAsync(bookingItem, ct);

                    createdLog = new UsageLog(
                        request.BookingItemId,
                        actualCheckin);

                    await _repository.AddAsync(createdLog, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.CheckIn,
                        entityName: nameof(UsageLog),
                        entityId: createdLog.LogId,
                        oldValue: null,
                        newValue: new
                        {
                            createdLog.LogId,
                            createdLog.BookingItemId,
                            createdLog.ActualCheckin,
                            Backdated = request.ActualCheckin.HasValue
                        },
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            return MapResponse(createdLog!);
        }

        public async Task<UsageLogResponse> CheckOutAsync(
            int logId,
            CheckOutUsageRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            UsageLog? updatedLog = null;
            int? lateBookingId = null;
            int? completedBookingId = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var actor = await GetAuthenticatedUserAsync(ct);

                    var usageLog = await GetLogOrThrowAsync(logId, ct);
                    var booking = await GetBookingForLogAsync(usageLog, ct);
                    await EnsureCanAccessBookingAsync(actor, booking, ct);

                    if (booking.Status != BookingStatus.Approved)
                    {
                        throw new InvalidOperationException(
                            "Chỉ booking đang Approved mới được checkout.");
                    }

                    DateTime actualCheckout = ResolveActualTime(
                        request.ActualCheckout,
                        actor,
                        "checkout");

                    var oldValue = new
                    {
                        usageLog.ActualCheckout,
                        IncidentStatus = usageLog.IncidentStatus.ToString(),
                        usageLog.IncidentDescription,
                        BookingStatus = booking.Status.ToString()
                    };

                    usageLog.CheckOut(actualCheckout);

                    if (actualCheckout > booking.EndTime)
                    {
                        usageLog.ReportIncident(
                            UsageIncidentStatus.LateCheckout,
                            "Checkout sau thời gian kết thúc booking.");
                        lateBookingId = booking.BookingId;
                    }

                    var bookingItem = booking.BookingItems.FirstOrDefault(
                        x => x.BookingItemId == usageLog.BookingItemId)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy BookingItem có ID {usageLog.BookingItemId}.");

                    await ReleaseResourceAsync(bookingItem, ct);
                    _repository.Update(usageLog);

                    var bookingLogs = await _repository.GetByBookingIdAsync(
                        booking.BookingId,
                        ct);

                    bool allItemsCheckedOut =
                        booking.BookingItems.Count > 0
                        && booking.BookingItems.All(item =>
                            bookingLogs.Any(log =>
                                log.BookingItemId == item.BookingItemId
                                && log.ActualCheckout.HasValue));

                    if (allItemsCheckedOut
                        && actualCheckout >= booking.StartTime
                        && booking.Status == BookingStatus.Approved)
                    {
                        booking.Complete();
                        completedBookingId = booking.BookingId;
                        _unitOfWork.Bookings.Update(booking);

                        await _unitOfWork.Notifications.AddAsync(
                            new Notification(
                                booking.UserId,
                                "Booking đã hoàn tất",
                                $"Tất cả tài nguyên của booking #{booking.BookingId} đã checkout.",
                                NotificationType.System),
                            ct);

                        await _auditLogWriter.WriteAsync(
                            actorUserId: actor.UserId,
                            actionType: AuditActionType.Update,
                            entityName: nameof(Booking),
                            entityId: booking.BookingId,
                            oldValue: new
                            {
                                Status = BookingStatus.Approved.ToString()
                            },
                            newValue: new
                            {
                                Status = booking.Status.ToString(),
                                Reason = "All booking items checked out"
                            },
                            cancellationToken: ct);
                    }

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.CheckOut,
                        entityName: nameof(UsageLog),
                        entityId: usageLog.LogId,
                        oldValue: oldValue,
                        newValue: new
                        {
                            usageLog.ActualCheckout,
                            IncidentStatus = usageLog.IncidentStatus.ToString(),
                            usageLog.IncidentDescription,
                            BookingStatus = booking.Status.ToString(),
                            Backdated = request.ActualCheckout.HasValue
                        },
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                    updatedLog = usageLog;
                },
                cancellationToken);

            if (lateBookingId.HasValue)
            {
                await _violationService.EnsureAutomaticViolationAsync(
                    lateBookingId.Value,
                    ViolationType.LateCheckout,
                    cancellationToken);
            }

            if (completedBookingId.HasValue)
            {
                await _waitlistService.NotifyNextForReleasedBookingAsync(
                    completedBookingId.Value,
                    cancellationToken);
            }

            return MapResponse(updatedLog!);
        }

        public async Task<UsageLogResponse> ReportIncidentAsync(
            int logId,
            ReportUsageIncidentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var usageLog = await GetLogOrThrowAsync(logId, cancellationToken);
            var booking = await GetBookingForLogAsync(
                usageLog,
                cancellationToken);

            await EnsureCanAccessBookingAsync(
                actor,
                booking,
                cancellationToken);

            if (!Enum.IsDefined(request.IncidentStatus)
                || request.IncidentStatus == UsageIncidentStatus.None)
            {
                throw new ArgumentException("Trạng thái sự cố không hợp lệ.");
            }

            var oldValue = new
            {
                IncidentStatus = usageLog.IncidentStatus.ToString(),
                usageLog.IncidentDescription
            };

            usageLog.ReportIncident(
                request.IncidentStatus,
                request.IncidentDescription);

            if (request.IncidentStatus is UsageIncidentStatus.DamageReported
                or UsageIncidentStatus.MissingEquipment)
            {
                var bookingItem = booking.BookingItems.FirstOrDefault(
                    x => x.BookingItemId == usageLog.BookingItemId)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy BookingItem có ID {usageLog.BookingItemId}.");

                if (bookingItem.EquipmentId.HasValue)
                {
                    var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                        bookingItem.EquipmentId.Value,
                        cancellationToken)
                        ?? throw new KeyNotFoundException(
                            $"Không tìm thấy thiết bị có ID {bookingItem.EquipmentId.Value}.");

                    equipment.MarkBroken();
                    _unitOfWork.Equipments.Update(equipment);

                    if (equipment.LabRoom?.ManagerId is int managerId)
                    {
                        await _unitOfWork.Notifications.AddAsync(
                            new Notification(
                                managerId,
                                "Thiết bị được báo cáo có sự cố",
                                $"Thiết bị #{equipment.EquipmentId} - {equipment.EquipmentName} "
                                + $"được báo cáo trong booking #{booking.BookingId}. "
                                + "Thiết bị đã chuyển sang Broken.",
                                NotificationType.Maintenance),
                            cancellationToken);
                    }
                }
            }

            _repository.Update(usageLog);

            await _auditLogWriter.WriteAsync(
                actorUserId: actor.UserId,
                actionType: AuditActionType.Update,
                entityName: nameof(UsageLog),
                entityId: usageLog.LogId,
                oldValue: oldValue,
                newValue: new
                {
                    IncidentStatus = usageLog.IncidentStatus.ToString(),
                    usageLog.IncidentDescription,
                    Action = "ReportIncident"
                },
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapResponse(usageLog);
        }

        private async Task MarkResourceInUseAsync(
            BookingItem bookingItem,
            CancellationToken cancellationToken)
        {
            if (bookingItem.ResourceType == ResourceType.LabRoom)
            {
                var lab = await _unitOfWork.LabRooms.GetDetailAsync(
                    bookingItem.LabId!.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {bookingItem.LabId.Value}.");

                if (lab.Status != LabRoomStatus.Available)
                {
                    throw new InvalidOperationException(
                        "Phòng lab hiện không khả dụng để check-in.");
                }

                return;
            }

            var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                bookingItem.EquipmentId!.Value,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {bookingItem.EquipmentId.Value}.");

            if (equipment.LabRoom?.Status != LabRoomStatus.Available)
            {
                throw new InvalidOperationException(
                    "Phòng chứa thiết bị hiện không khả dụng.");
            }

            equipment.MarkInUse();
            _unitOfWork.Equipments.Update(equipment);
        }

        private async Task ReleaseResourceAsync(
            BookingItem bookingItem,
            CancellationToken cancellationToken)
        {
            if (bookingItem.ResourceType != ResourceType.Equipment)
                return;

            var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                bookingItem.EquipmentId!.Value,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {bookingItem.EquipmentId.Value}.");

            equipment.MarkAvailable();
            _unitOfWork.Equipments.Update(equipment);
        }

        private static DateTime ResolveActualTime(
            DateTime? clientTime,
            User actor,
            string actionName)
        {
            DateTime now = DateTime.UtcNow;
            if (!clientTime.HasValue)
                return now;

            bool privileged = actor.Role?.RoleName is
                RoleName.Admin or RoleName.LabManager;

            if (!privileged)
            {
                throw new UnauthorizedAccessException(
                    $"Chỉ Admin hoặc LabManager được nhập thời gian {actionName} thủ công.");
            }

            if (clientTime.Value > now.Add(FutureClockTolerance))
            {
                throw new ArgumentException(
                    $"Thời gian {actionName} không được nằm trong tương lai.");
            }

            return clientTime.Value;
        }

        private async Task<UsageLog> GetLogOrThrowAsync(
            int logId,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(logId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy UsageLog có ID {logId}.");
        }

        private async Task<Booking> GetBookingForLogAsync(
            UsageLog usageLog,
            CancellationToken cancellationToken)
        {
            var bookingItem = await _unitOfWork.BookingItems.GetByIdAsync(
                usageLog.BookingItemId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy BookingItem có ID {usageLog.BookingItemId}.");

            return await _unitOfWork.Bookings.GetDetailAsync(
                bookingItem.BookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingItem.BookingId}.");
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var user = await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");

            user.TryUnlockExpiredRestriction(DateTime.UtcNow);

            if (user.Status is UserStatus.Inactive or UserStatus.Locked)
            {
                throw new InvalidOperationException(
                    "Tài khoản không được phép thao tác.");
            }

            return user;
        }

        private async Task<User> GetCurrentActiveUserAsync(
            CancellationToken cancellationToken)
        {
            var user = await GetAuthenticatedUserAsync(cancellationToken);
            if (user.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Tài khoản phải ở trạng thái Active.");
            }

            return user;
        }

        private static void EnsureManagerOrAdmin(User actor)
        {
            if (actor.Role?.RoleName is not RoleName.Admin
                and not RoleName.LabManager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được truy cập dữ liệu này.");
            }
        }

        private async Task EnsureCanAccessBookingAsync(
            User actor,
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (actor.UserId == booking.UserId
                || actor.Role?.RoleName == RoleName.Admin)
            {
                return;
            }

            if (actor.Role?.RoleName == RoleName.LabManager)
            {
                var managedLabIds = await GetManagedLabIdsAsync(
                    actor.UserId,
                    cancellationToken);

                if (BookingBelongsToManagedLabs(booking, managedLabIds))
                    return;
            }

            throw new UnauthorizedAccessException(
                "Bạn không có quyền truy cập UsageLog của booking này.");
        }

        private async Task<HashSet<int>> GetManagedLabIdsAsync(
            int managerId,
            CancellationToken cancellationToken)
        {
            var labs = await _unitOfWork.LabRooms.GetByManagerIdAsync(
                managerId,
                cancellationToken);

            return labs.Select(x => x.LabId).ToHashSet();
        }

        private static bool BookingBelongsToManagedLabs(
            Booking booking,
            HashSet<int> managedLabIds)
        {
            return booking.BookingItems.Count > 0
                && booking.BookingItems.All(item =>
                {
                    int? labId = item.LabId ?? item.Equipment?.LabId;
                    return labId.HasValue
                        && managedLabIds.Contains(labId.Value);
                });
        }

        private static UsageLogResponse MapResponse(UsageLog usageLog)
        {
            return new UsageLogResponse
            {
                LogId = usageLog.LogId,
                BookingItemId = usageLog.BookingItemId,
                ActualCheckin = usageLog.ActualCheckin,
                ActualCheckout = usageLog.ActualCheckout,
                IncidentStatus = usageLog.IncidentStatus.ToString(),
                IncidentDescription = usageLog.IncidentDescription
            };
        }
    }
}
