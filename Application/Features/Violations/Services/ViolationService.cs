using Application.DTOs.Violations;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using AutoMapper;

namespace Application.Services
{
    public class ViolationService : IViolationService
    {
        private const int RestrictionPointThreshold = 10;
        private const int RestrictionDurationDays = 7;

        private readonly IViolationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public ViolationService(
            IMapper mapper,
            IViolationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService)
        {
            _mapper = mapper;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
        }

        public async Task<List<ViolationResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            IReadOnlyList<Violation> violations =
                actor.Role?.RoleName == RoleName.LabManager
                    ? await _repository.GetByManagerIdAsync(
                        actor.UserId,
                        userId: null,
                        activeOnly: false,
                        cancellationToken: cancellationToken)
                    : await _repository.GetAllAsync(cancellationToken);

            return violations
                .OrderByDescending(x => x.LoggedAt)
                .Select(violation => _mapper.Map<ViolationResponse>(violation))
                .ToList();
        }

        public async Task<ViolationResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var violation = await _repository.GetByIdAsync(id, cancellationToken);

            if (violation is null)
                return null;

            var booking = await GetBookingOrThrowAsync(
                violation.BookingId,
                cancellationToken);
            await EnsureCanReadBookingAsync(actor, booking, cancellationToken);

            return _mapper.Map<ViolationResponse>(violation);
        }

        public async Task<List<ViolationResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            await GetUserOrThrowAsync(userId, cancellationToken);

            IReadOnlyList<Violation> violations;
            if (actor.UserId == userId
                || actor.Role?.RoleName == RoleName.Admin)
            {
                violations = await _repository.GetByUserIdAsync(
                    userId,
                    cancellationToken);
            }
            else if (actor.Role?.RoleName == RoleName.LabManager)
            {
                violations = await _repository.GetByManagerIdAsync(
                    actor.UserId,
                    userId: userId,
                    activeOnly: false,
                    cancellationToken: cancellationToken);
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền xem vi phạm của người dùng này.");
            }

            return _mapper.Map<List<ViolationResponse>>(violations);
        }

        public async Task<List<ViolationResponse>> GetActiveByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            await GetUserOrThrowAsync(userId, cancellationToken);

            IReadOnlyList<Violation> violations;
            if (actor.UserId == userId
                || actor.Role?.RoleName == RoleName.Admin)
            {
                violations = await _repository.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);
            }
            else if (actor.Role?.RoleName == RoleName.LabManager)
            {
                violations = await _repository.GetByManagerIdAsync(
                    actor.UserId,
                    userId: userId,
                    activeOnly: true,
                    cancellationToken: cancellationToken);
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền xem vi phạm của người dùng này.");
            }

            return _mapper.Map<List<ViolationResponse>>(violations);
        }

        public async Task<List<ViolationResponse>> GetByBookingIdAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var booking = await GetBookingOrThrowAsync(
                bookingId,
                cancellationToken);

            await EnsureCanReadBookingAsync(actor, booking, cancellationToken);

            var violations = await _repository.GetByBookingIdAsync(
                bookingId,
                cancellationToken);

            return _mapper.Map<List<ViolationResponse>>(violations);
        }

        public async Task<UserViolationSummaryResponse> GetUserSummaryAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var user = await GetUserOrThrowAsync(userId, cancellationToken);

            IReadOnlyList<Violation> activeViolations;
            if (actor.UserId == userId
                || actor.Role?.RoleName == RoleName.Admin)
            {
                activeViolations = await _repository.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);
            }
            else if (actor.Role?.RoleName == RoleName.LabManager)
            {
                activeViolations = await _repository.GetByManagerIdAsync(
                    actor.UserId,
                    userId: userId,
                    activeOnly: true,
                    cancellationToken: cancellationToken);
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền xem tổng hợp vi phạm của người dùng này.");
            }

            return new UserViolationSummaryResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                PenaltyPoints = user.PenaltyPoints,
                UserStatus = user.Status.ToString(),
                RestrictionUntil = user.RestrictionUntil,
                ActiveViolationCount = activeViolations.Count,
                ActivePenaltyPoints = activeViolations.Sum(
                    x => x.PenaltyPointsAdded),
                ActiveViolations = activeViolations
                    .Select(violation =>
                        _mapper.Map<ViolationResponse>(violation))
                    .ToList()
            };
        }

        public async Task<ViolationResponse> CreateAsync(
            CreateViolationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            Violation? createdViolation = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var actor = await GetCurrentActiveUserAsync(ct);
                    EnsureManagerOrAdmin(actor);

                    var booking = await GetBookingOrThrowAsync(
                        request.BookingId,
                        ct);
                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    if (booking.UserId != request.UserId)
                    {
                        throw new ArgumentException(
                            "Booking không thuộc người dùng được ghi nhận vi phạm.");
                    }

                    await CreateViolationCoreAsync(
                        actor.UserId,
                        booking,
                        request.ViolationType,
                        throwWhenDuplicate: true,
                        ct,
                        violation => createdViolation = violation);
                },
                cancellationToken);

            return _mapper.Map<ViolationResponse>(createdViolation!);
        }

        public async Task<ViolationResponse?> EnsureAutomaticViolationAsync(
            int bookingId,
            ViolationType violationType,
            CancellationToken cancellationToken)
        {
            if (violationType is not ViolationType.NoShow
                and not ViolationType.LateCheckout)
            {
                throw new ArgumentException(
                    "Chỉ NoShow và LateCheckout được tự động tạo từ nghiệp vụ.");
            }

            Violation? result = null;

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var actor = await GetAuthenticatedUserAsync(ct);
                    var booking = await GetBookingOrThrowAsync(bookingId, ct);

                    var existing = await _repository.FindAsync(
                        x => x.UserId == booking.UserId
                             && x.BookingId == booking.BookingId
                             && x.ViolationType == violationType
                             && x.Status == ViolationStatus.Active,
                        ct);

                    if (existing.Count > 0)
                    {
                        result = existing
                            .OrderByDescending(x => x.LoggedAt)
                            .First();
                        return;
                    }

                    await CreateViolationCoreAsync(
                        actor.UserId,
                        booking,
                        violationType,
                        throwWhenDuplicate: false,
                        ct,
                        violation => result = violation);
                },
                cancellationToken);

            return result is null
                ? null
                : _mapper.Map<ViolationResponse>(result);
        }

        public async Task ResolveAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await ChangeStatusAsync(
                id,
                resolve: true,
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await ChangeStatusAsync(
                id,
                resolve: false,
                cancellationToken);
        }

        private async Task CreateViolationCoreAsync(
            int actorUserId,
            Booking booking,
            ViolationType violationType,
            bool throwWhenDuplicate,
            CancellationToken cancellationToken,
            Action<Violation> capture)
        {
            if (!Enum.IsDefined(violationType))
                throw new ArgumentException("Loại vi phạm không hợp lệ.");

            var targetUser = await GetUserOrThrowAsync(
                booking.UserId,
                cancellationToken);

            if (targetUser.Status is UserStatus.Inactive or UserStatus.Locked)
            {
                throw new InvalidOperationException(
                    "Không thể ghi nhận vi phạm cho người dùng đang Inactive hoặc Locked.");
            }

            bool duplicateExists = await _repository.ExistsAsync(
                x => x.UserId == booking.UserId
                     && x.BookingId == booking.BookingId
                     && x.ViolationType == violationType
                     && x.Status == ViolationStatus.Active,
                cancellationToken);

            if (duplicateExists)
            {
                if (throwWhenDuplicate)
                {
                    throw new InvalidOperationException(
                        "Vi phạm này đã được ghi nhận và vẫn đang Active.");
                }

                return;
            }

            await ValidateEvidenceAsync(
                booking,
                violationType,
                cancellationToken);

            int penaltyPoints = GetPenaltyPoints(violationType);
            var violation = new Violation(
                booking.UserId,
                booking.BookingId,
                violationType,
                penaltyPoints);

            var oldUserValue = new
            {
                targetUser.PenaltyPoints,
                Status = targetUser.Status.ToString(),
                targetUser.RestrictionUntil
            };

            targetUser.AddPenaltyPoints(penaltyPoints);
            ApplyRestrictionPolicy(targetUser);

            await _repository.AddAsync(violation, cancellationToken);
            _unitOfWork.Users.Update(targetUser);

            await _unitOfWork.Notifications.AddAsync(
                new Notification(
                    targetUser.UserId,
                    "Ghi nhận vi phạm",
                    $"Bạn bị ghi nhận vi phạm {violationType} "
                    + $"và cộng {penaltyPoints} điểm phạt.",
                    NotificationType.Violation),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogWriter.WriteAsync(
                actorUserId: actorUserId,
                actionType: AuditActionType.Create,
                entityName: nameof(Violation),
                entityId: violation.ViolationId,
                oldValue: new { User = oldUserValue },
                newValue: new
                {
                    violation.ViolationId,
                    violation.UserId,
                    violation.BookingId,
                    ViolationType = violation.ViolationType.ToString(),
                    violation.PenaltyPointsAdded,
                    ViolationStatus = violation.Status.ToString(),
                    User = new
                    {
                        targetUser.PenaltyPoints,
                        Status = targetUser.Status.ToString(),
                        targetUser.RestrictionUntil
                    }
                },
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            capture(violation);
        }

        private async Task ChangeStatusAsync(
            int id,
            bool resolve,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var actor = await GetCurrentActiveUserAsync(ct);
                    EnsureManagerOrAdmin(actor);

                    var violation = await GetViolationOrThrowAsync(id, ct);
                    var booking = await GetBookingOrThrowAsync(
                        violation.BookingId,
                        ct);
                    await EnsureManagerCanManageBookingAsync(actor, booking, ct);

                    var targetUser = await GetUserOrThrowAsync(
                        violation.UserId,
                        ct);

                    var oldValue = new
                    {
                        ViolationStatus = violation.Status.ToString(),
                        UserPenaltyPoints = targetUser.PenaltyPoints,
                        UserStatus = targetUser.Status.ToString(),
                        targetUser.RestrictionUntil
                    };

                    if (resolve)
                        violation.Resolve();
                    else
                        violation.Cancel();

                    if (!resolve)
                    {
                        targetUser.RemovePenaltyPoints(
                            violation.PenaltyPointsAdded);
                        ReleaseRestrictionWhenEligible(targetUser);
                    }

                    _repository.Update(violation);
                    _unitOfWork.Users.Update(targetUser);

                    await _unitOfWork.Notifications.AddAsync(
                        new Notification(
                            targetUser.UserId,
                            resolve
                                ? "Vi phạm đã được xử lý"
                                : "Hủy ghi nhận vi phạm",
                            resolve
                                ? $"Vi phạm #{violation.ViolationId} đã chuyển sang Resolved."
                                : $"Vi phạm #{violation.ViolationId} đã bị hủy.",
                            NotificationType.Violation),
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Violation),
                        entityId: violation.ViolationId,
                        oldValue: oldValue,
                        newValue: new
                        {
                            ViolationStatus = violation.Status.ToString(),
                            UserPenaltyPoints = targetUser.PenaltyPoints,
                            UserStatus = targetUser.Status.ToString(),
                            targetUser.RestrictionUntil,
                            Action = resolve ? "Resolve" : "Cancel"
                        },
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        private async Task ValidateEvidenceAsync(
            Booking booking,
            ViolationType violationType,
            CancellationToken cancellationToken)
        {
            if (violationType == ViolationType.NoShow)
            {
                if (booking.Status != BookingStatus.NoShow)
                {
                    throw new InvalidOperationException(
                        "Booking phải ở trạng thái NoShow trước khi ghi nhận vi phạm NoShow.");
                }

                return;
            }

            if (violationType == ViolationType.LateCheckout)
            {
                var usageLogs = await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                    booking.BookingId,
                    cancellationToken);

                bool hasLateCheckout = usageLogs.Any(x =>
                    x.IncidentStatus == UsageIncidentStatus.LateCheckout
                    || (x.ActualCheckout.HasValue
                        && x.ActualCheckout.Value > booking.EndTime));

                if (!hasLateCheckout)
                {
                    throw new InvalidOperationException(
                        "Chưa có UsageLog chứng minh người dùng checkout muộn.");
                }

                return;
            }

            if (violationType == ViolationType.DamageEquipment)
            {
                var usageLogs = await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                    booking.BookingId,
                    cancellationToken);

                bool hasDamageReport = usageLogs.Any(x =>
                    (x.IncidentStatus == UsageIncidentStatus.DamageReported
                        || x.IncidentStatus == UsageIncidentStatus.MissingEquipment)
                    && x.IncidentReviewStatus
                        == IncidentReviewStatus.Confirmed);

                if (!hasDamageReport)
                {
                    throw new InvalidOperationException(
                        "Chưa có UsageLog ghi nhận hư hỏng hoặc thất lạc thiết bị.");
                }
            }
        }

        private static int GetPenaltyPoints(ViolationType violationType)
        {
            return violationType switch
            {
                ViolationType.NoShow => 5,
                ViolationType.LateCheckout => 3,
                ViolationType.DamageEquipment => 6,
                ViolationType.MisuseEquipment => 4,
                ViolationType.UnauthorizedUse => 7,
                _ => throw new ArgumentException("Loại vi phạm không hợp lệ.")
            };
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var user = await GetUserOrThrowAsync(userId, cancellationToken);
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

        private async Task<User> GetUserOrThrowAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            return await _unitOfWork.Users.GetUserByIdAsync(
                userId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
        }

        private async Task<Booking> GetBookingOrThrowAsync(
            int bookingId,
            CancellationToken cancellationToken)
        {
            return await _unitOfWork.Bookings.GetDetailAsync(
                bookingId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {bookingId}.");
        }

        private async Task<Violation> GetViolationOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy vi phạm có ID {id}.");
        }

        private static void EnsureManagerOrAdmin(User actor)
        {
            if (actor.Role?.RoleName is not RoleName.Admin
                and not RoleName.LabManager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được quản lý vi phạm.");
            }
        }

        private async Task EnsureCanReadBookingAsync(
            User actor,
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (actor.UserId == booking.UserId
                || actor.Role?.RoleName == RoleName.Admin)
            {
                return;
            }

            if (actor.Role?.RoleName == RoleName.LabManager
                && await CanManageBookingAsync(
                    actor.UserId,
                    booking,
                    cancellationToken))
            {
                return;
            }

            throw new UnauthorizedAccessException(
                "Bạn không có quyền xem vi phạm của booking này.");
        }

        private async Task EnsureManagerCanManageBookingAsync(
            User actor,
            Booking booking,
            CancellationToken cancellationToken)
        {
            if (actor.Role?.RoleName == RoleName.Admin)
                return;

            if (actor.Role?.RoleName != RoleName.LabManager
                || !await CanManageBookingAsync(
                    actor.UserId,
                    booking,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được quản lý vi phạm của phòng mình phụ trách.");
            }
        }

        private async Task<bool> CanManageBookingAsync(
            int managerId,
            Booking booking,
            CancellationToken cancellationToken)
        {
            var labs = await _unitOfWork.LabRooms.GetByManagerIdAsync(
                managerId,
                cancellationToken);

            return BookingBelongsToManagedLabs(
                booking,
                labs.Select(x => x.LabId).ToHashSet());
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

        private static void ApplyRestrictionPolicy(User user)
        {
            if (user.PenaltyPoints < RestrictionPointThreshold)
                return;

            DateTime baseTime =
                user.RestrictionUntil.HasValue
                && user.RestrictionUntil.Value > DateTime.UtcNow
                    ? user.RestrictionUntil.Value
                    : DateTime.UtcNow;

            user.RestrictUntil(baseTime.AddDays(RestrictionDurationDays));
        }

        private static void ReleaseRestrictionWhenEligible(User user)
        {
            if (user.Status == UserStatus.Restricted
                && user.PenaltyPoints < RestrictionPointThreshold)
            {
                user.Unlock();
            }
        }

    }
}
