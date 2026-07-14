using Application.DTOs.Violations;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class ViolationService : IViolationService
    {
        private const int RestrictionPointThreshold = 10;
        private const int RestrictionDurationDays = 7;

        private readonly IViolationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public ViolationService(
            IViolationRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ViolationResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var violations = await _repository.GetAllAsync(
                cancellationToken);

            return violations
                .OrderByDescending(x => x.LoggedAt)
                .Select(MapResponse)
                .ToList();
        }

        public async Task<ViolationResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var violation = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            return violation is null
                ? null
                : MapResponse(violation);
        }

        public async Task<List<ViolationResponse>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            var violations =
                await _repository.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            return violations
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<ViolationResponse>> GetActiveByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            var violations =
                await _repository.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);

            return violations
                .Select(MapResponse)
                .ToList();
        }

        public async Task<List<ViolationResponse>> GetByBookingIdAsync(
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

            var violations =
                await _repository.GetByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            return violations
                .Select(MapResponse)
                .ToList();
        }

        public async Task<UserViolationSummaryResponse> GetUserSummaryAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            var activeViolations =
                await _repository.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);

            int activeCount =
                await _repository.CountActiveViolationsAsync(
                    userId,
                    cancellationToken);

            int activePenaltyPoints =
                await _repository.GetTotalActivePenaltyPointsAsync(
                    userId,
                    cancellationToken);

            return new UserViolationSummaryResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                PenaltyPoints = user.PenaltyPoints,
                UserStatus = user.Status.ToString(),
                RestrictionUntil = user.RestrictionUntil,
                ActiveViolationCount = activeCount,
                ActivePenaltyPoints = activePenaltyPoints,
                ActiveViolations = activeViolations
                    .Select(MapResponse)
                    .ToList()
            };
        }

        public async Task<ViolationResponse> CreateAsync(
            CreateViolationRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateActorAsync(
                request.ActorUserId,
                cancellationToken);

            var targetUser = await GetUserOrThrowAsync(
                request.UserId,
                cancellationToken);

            if (targetUser.Status == UserStatus.Inactive
                || targetUser.Status == UserStatus.Locked)
            {
                throw new InvalidOperationException(
                    "Không thể ghi nhận vi phạm cho người dùng đang Inactive hoặc Locked.");
            }

            var booking =
                await _unitOfWork.Bookings.GetDetailAsync(
                    request.BookingId,
                    cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy booking có ID {request.BookingId}.");
            }

            if (booking.UserId != request.UserId)
            {
                throw new ArgumentException(
                    "Booking không thuộc người dùng được ghi nhận vi phạm.");
            }

            if (!Enum.IsDefined(
                    typeof(ViolationType),
                    request.ViolationType))
            {
                throw new ArgumentException(
                    "Loại vi phạm không hợp lệ.");
            }

            if (request.PenaltyPointsAdded <= 0)
            {
                throw new ArgumentException(
                    "Điểm phạt phải lớn hơn 0.");
            }

            bool duplicateExists =
                await _repository.ExistsAsync(
                    x => x.UserId == request.UserId
                         && x.BookingId == request.BookingId
                         && x.ViolationType
                            == request.ViolationType
                         && x.Status
                            == ViolationStatus.Active,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new InvalidOperationException(
                    "Vi phạm này đã được ghi nhận và vẫn đang Active.");
            }

            await ValidateEvidenceAsync(
                booking,
                request.ViolationType,
                cancellationToken);

            var violation = new Violation(
                request.UserId,
                request.BookingId,
                request.ViolationType,
                request.PenaltyPointsAdded);

            targetUser.AddPenaltyPoints(
                request.PenaltyPointsAdded);

            ApplyRestrictionPolicy(targetUser);

            var notification = new Notification(
                targetUser.UserId,
                "Ghi nhận vi phạm",
                $"Bạn bị ghi nhận vi phạm {request.ViolationType} "
                + $"và cộng {request.PenaltyPointsAdded} điểm phạt.",
                NotificationType.Violation);

            await _repository.AddAsync(
                violation,
                cancellationToken);

            _unitOfWork.Users.Update(targetUser);

            await _unitOfWork.Notifications.AddAsync(
                notification,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapResponse(violation);
        }

        public async Task ResolveAsync(
            int id,
            ViolationActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateActorAsync(
                request.ActorUserId,
                cancellationToken);

            var violation = await GetViolationOrThrowAsync(
                id,
                cancellationToken);

            var targetUser = await GetUserOrThrowAsync(
                violation.UserId,
                cancellationToken);

            violation.Resolve();

            targetUser.RemovePenaltyPoints(
                violation.PenaltyPointsAdded);

            ReleaseRestrictionWhenEligible(targetUser);

            var notification = new Notification(
                targetUser.UserId,
                "Vi phạm đã được xử lý",
                $"Vi phạm #{violation.ViolationId} đã chuyển sang Resolved.",
                NotificationType.Violation);

            _repository.Update(violation);
            _unitOfWork.Users.Update(targetUser);

            await _unitOfWork.Notifications.AddAsync(
                notification,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            ViolationActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await ValidateActorAsync(
                request.ActorUserId,
                cancellationToken);

            var violation = await GetViolationOrThrowAsync(
                id,
                cancellationToken);

            var targetUser = await GetUserOrThrowAsync(
                violation.UserId,
                cancellationToken);

            violation.Cancel();

            targetUser.RemovePenaltyPoints(
                violation.PenaltyPointsAdded);

            ReleaseRestrictionWhenEligible(targetUser);

            var notification = new Notification(
                targetUser.UserId,
                "Hủy ghi nhận vi phạm",
                $"Vi phạm #{violation.ViolationId} đã bị hủy.",
                NotificationType.Violation);

            _repository.Update(violation);
            _unitOfWork.Users.Update(targetUser);

            await _unitOfWork.Notifications.AddAsync(
                notification,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task ValidateActorAsync(
            int actorUserId,
            CancellationToken cancellationToken)
        {
            var actor = await _unitOfWork.Users.GetUserByIdAsync(
                actorUserId,
                cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy người thực hiện có ID {actorUserId}.");
            }

            if (actor.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Người thực hiện hiện không ở trạng thái Active.");
            }

            bool hasPermission =
                actor.Role?.RoleName == RoleName.Admin
                || actor.Role?.RoleName == RoleName.LabManager;

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được quản lý vi phạm.");
            }
        }

        private async Task<User> GetUserOrThrowAsync(
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

            return user;
        }

        private async Task<Violation> GetViolationOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var violation = await _repository.GetByIdAsync(
                id,
                cancellationToken);

            if (violation is null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy vi phạm có ID {id}.");
            }

            return violation;
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
                var usageLogs =
                    await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                        booking.BookingId,
                        cancellationToken);

                bool hasLateCheckout = usageLogs.Any(
                    x => x.IncidentStatus
                            == UsageIncidentStatus.LateCheckout
                         || (x.ActualCheckout.HasValue
                             && x.ActualCheckout.Value
                                > booking.EndTime));

                if (!hasLateCheckout)
                {
                    throw new InvalidOperationException(
                        "Chưa có UsageLog chứng minh người dùng checkout muộn.");
                }

                return;
            }

            if (violationType == ViolationType.DamageEquipment)
            {
                var usageLogs =
                    await _unitOfWork.UsageLogs.GetByBookingIdAsync(
                        booking.BookingId,
                        cancellationToken);

                bool hasDamageReport = usageLogs.Any(
                    x => x.IncidentStatus
                            == UsageIncidentStatus.DamageReported
                         || x.IncidentStatus
                            == UsageIncidentStatus.MissingEquipment);

                if (!hasDamageReport)
                {
                    throw new InvalidOperationException(
                        "Chưa có UsageLog ghi nhận hư hỏng hoặc thất lạc thiết bị.");
                }
            }
        }

        private static void ApplyRestrictionPolicy(User user)
        {
            if (user.PenaltyPoints < RestrictionPointThreshold)
            {
                return;
            }

            DateTime baseTime =
                user.RestrictionUntil.HasValue
                && user.RestrictionUntil.Value > DateTime.UtcNow
                    ? user.RestrictionUntil.Value
                    : DateTime.UtcNow;

            user.RestrictUntil(
                baseTime.AddDays(RestrictionDurationDays));
        }

        private static void ReleaseRestrictionWhenEligible(
            User user)
        {
            if (user.Status == UserStatus.Restricted
                && user.PenaltyPoints
                    < RestrictionPointThreshold)
            {
                user.Unlock();
            }
        }

        private static ViolationResponse MapResponse(
            Violation violation)
        {
            return new ViolationResponse
            {
                ViolationId = violation.ViolationId,
                UserId = violation.UserId,
                BookingId = violation.BookingId,
                ViolationType =
                    violation.ViolationType.ToString(),
                PenaltyPointsAdded =
                    violation.PenaltyPointsAdded,
                LoggedAt = violation.LoggedAt,
                Status = violation.Status.ToString()
            };
        }
    }

}
