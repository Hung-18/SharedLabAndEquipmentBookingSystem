using Application.DTOs.Reports;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class ReportService : IReportService
    {
        private const int MaximumTop = 100;
        private static readonly TimeSpan MaximumRange = TimeSpan.FromDays(366);

        private readonly IReportRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly ILabRoomRepository _labRoomRepository;
        private readonly ICurrentUserService _currentUserService;

        public ReportService(
            IReportRepository repository,
            IUserRepository userRepository,
            ILabRoomRepository labRoomRepository,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _userRepository = userRepository;
            _labRoomRepository = labRoomRepository;
            _currentUserService = currentUserService;
        }

        public async Task<List<ResourceUtilizationResponse>> GetLabUtilizationAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            var labs = await _repository.GetLabRoomsAsync(
                scope.AllowedLabIds,
                cancellationToken);
            var bookings = await _repository.GetBookingsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);
            var logs = await _repository.GetUsageLogsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);
            double availableHours = (to - from).TotalHours;

            return labs.Select(lab =>
            {
                var matchingBookings = bookings
                    .Where(IsUtilizedBooking)
                    .Where(x => BookingTouchesLab(x, lab.LabId))
                    .GroupBy(x => x.BookingId)
                    .Select(x => x.First())
                    .ToList();

                var matchingLogs = logs
                    .Where(x => UsageLogTouchesLab(x, lab.LabId))
                    .GroupBy(x => x.LogId)
                    .Select(x => x.First())
                    .ToList();

                double reservedHours = MergeAndMeasure(
                    matchingBookings.Select(x => (x.StartTime, x.EndTime)),
                    from,
                    to);

                double actualUsageHours = MergeAndMeasure(
                    matchingLogs.Select(x =>
                        (x.ActualCheckin, ResolveCheckout(x, to))),
                    from,
                    to);

                return new ResourceUtilizationResponse
                {
                    ResourceType = ResourceType.LabRoom.ToString(),
                    ResourceId = lab.LabId,
                    ResourceName = lab.LabName,
                    LabId = lab.LabId,
                    LabName = lab.LabName,
                    BookingCount = matchingBookings.Count,
                    ReservedHours = Round(reservedHours),
                    UsageCount = matchingLogs.Count,
                    ActualUsageHours = Round(actualUsageHours),
                    AvailableHours = Round(availableHours),
                    UtilizationRate = Percentage(actualUsageHours, availableHours)
                };
            })
            .OrderByDescending(x => x.UtilizationRate)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        public async Task<List<ResourceUtilizationResponse>> GetEquipmentUtilizationAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            var equipments = await _repository.GetEquipmentsAsync(
                scope.AllowedLabIds,
                cancellationToken);
            var bookings = await _repository.GetBookingsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);
            var logs = await _repository.GetUsageLogsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);
            double availableHours = (to - from).TotalHours;

            return equipments.Select(equipment =>
            {
                var matchingBookings = bookings
                    .Where(IsUtilizedBooking)
                    .Where(x => x.BookingItems.Any(item =>
                        item.EquipmentId == equipment.EquipmentId))
                    .GroupBy(x => x.BookingId)
                    .Select(x => x.First())
                    .ToList();

                var matchingLogs = logs
                    .Where(x => x.BookingItem?.EquipmentId == equipment.EquipmentId)
                    .GroupBy(x => x.LogId)
                    .Select(x => x.First())
                    .ToList();

                double reservedHours = MergeAndMeasure(
                    matchingBookings.Select(x => (x.StartTime, x.EndTime)),
                    from,
                    to);

                double actualUsageHours = MergeAndMeasure(
                    matchingLogs.Select(x =>
                        (x.ActualCheckin, ResolveCheckout(x, to))),
                    from,
                    to);

                return new ResourceUtilizationResponse
                {
                    ResourceType = ResourceType.Equipment.ToString(),
                    ResourceId = equipment.EquipmentId,
                    ResourceName = equipment.EquipmentName,
                    LabId = equipment.LabId,
                    LabName = equipment.LabRoom?.LabName,
                    BookingCount = matchingBookings.Count,
                    ReservedHours = Round(reservedHours),
                    UsageCount = matchingLogs.Count,
                    ActualUsageHours = Round(actualUsageHours),
                    AvailableHours = Round(availableHours),
                    UtilizationRate = Percentage(actualUsageHours, availableHours)
                };
            })
            .OrderByDescending(x => x.UtilizationRate)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        public async Task<List<CategoryCountResponse>> GetBookingsByDepartmentAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(from, to, cancellationToken);
            return BuildCategoryCounts(
                bookings.GroupBy(x => new
                {
                    Id = x.User?.DepartmentId ?? 0,
                    Name = x.User?.Department?.DepartmentName ?? "Không xác định"
                })
                .Select(x => (x.Key.Id.ToString(), x.Key.Name, x.Count())));
        }

        public async Task<List<CategoryCountResponse>> GetBookingsByPurposeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(from, to, cancellationToken);
            return BuildCategoryCounts(
                bookings.GroupBy(x => x.PurposeType)
                    .Select(x => (((int)x.Key).ToString(), x.Key.ToString(), x.Count())));
        }

        public async Task<List<CategoryCountResponse>> GetBookingsByStatusAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(from, to, cancellationToken);
            var counts = Enum.GetValues<BookingStatus>()
                .Select(status =>
                {
                    int count = bookings.Count(x => x.Status == status);
                    return (((int)status).ToString(), status.ToString(), count);
                });

            return BuildCategoryCounts(counts, includeZero: true);
        }

        public async Task<List<MaintenanceCostResponse>> GetMaintenanceCostsByLabAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            var labs = await _repository.GetLabRoomsAsync(scope.AllowedLabIds, cancellationToken);
            var maintenances = await _repository.GetMaintenancesAsync(from, to, scope.AllowedLabIds, cancellationToken);

            return labs.Select(lab =>
            {
                var matching = maintenances
                    .Where(x => x.Status != MaintenanceStatus.Cancelled)
                    .Where(x => x.LabId == lab.LabId
                        || x.Equipment?.LabId == lab.LabId)
                    .ToList();

                return new MaintenanceCostResponse
                {
                    ResourceType = ResourceType.LabRoom.ToString(),
                    ResourceId = lab.LabId,
                    ResourceName = lab.LabName,
                    LabId = lab.LabId,
                    LabName = lab.LabName,
                    MaintenanceCount = matching.Count,
                    TotalCost = matching.Sum(x => x.MaintenanceCost)
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        public async Task<List<MaintenanceCostResponse>> GetMaintenanceCostsByEquipmentAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            var equipments = await _repository.GetEquipmentsAsync(scope.AllowedLabIds, cancellationToken);
            var maintenances = await _repository.GetMaintenancesAsync(from, to, scope.AllowedLabIds, cancellationToken);

            return equipments.Select(equipment =>
            {
                var matching = maintenances
                    .Where(x => x.Status != MaintenanceStatus.Cancelled)
                    .Where(x => x.EquipmentId == equipment.EquipmentId)
                    .ToList();

                return new MaintenanceCostResponse
                {
                    ResourceType = ResourceType.Equipment.ToString(),
                    ResourceId = equipment.EquipmentId,
                    ResourceName = equipment.EquipmentName,
                    LabId = equipment.LabId,
                    LabName = equipment.LabRoom?.LabName,
                    MaintenanceCount = matching.Count,
                    TotalCost = matching.Sum(x => x.MaintenanceCost)
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        public async Task<List<MostUsedResourceResponse>> GetMostUsedLabRoomsAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default)
        {
            ValidateTop(top);
            var utilization = await GetLabUtilizationAsync(from, to, cancellationToken);
            return utilization
                .OrderByDescending(x => x.UsageCount)
                .ThenByDescending(x => x.ActualUsageHours)
                .Take(top)
                .Select(ToMostUsed)
                .ToList();
        }

        public async Task<List<MostUsedResourceResponse>> GetMostUsedEquipmentsAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default)
        {
            ValidateTop(top);
            var utilization = await GetEquipmentUtilizationAsync(from, to, cancellationToken);
            return utilization
                .OrderByDescending(x => x.UsageCount)
                .ThenByDescending(x => x.ActualUsageHours)
                .Take(top)
                .Select(ToMostUsed)
                .ToList();
        }

        public async Task<ViolationSummaryResponse> GetViolationsAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            var violations = await _repository.GetViolationsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            var items = violations.Select(x => new ViolationReportResponse
            {
                ViolationId = x.ViolationId,
                UserId = x.UserId,
                UserName = x.User?.FullName ?? string.Empty,
                DepartmentName = x.User?.Department?.DepartmentName ?? string.Empty,
                BookingId = x.BookingId,
                ViolationType = x.ViolationType.ToString(),
                PenaltyPointsAdded = x.PenaltyPointsAdded,
                Status = x.Status.ToString(),
                LoggedAt = x.LoggedAt
            }).ToList();

            return new ViolationSummaryResponse
            {
                TotalCount = items.Count,
                ActiveCount = violations.Count(x => x.Status == ViolationStatus.Active),
                ResolvedCount = violations.Count(x => x.Status == ViolationStatus.Resolved),
                CancelledCount = violations.Count(x => x.Status == ViolationStatus.Cancelled),
                ViolationTypeCounts = BuildCategoryCounts(
                    violations
                        .GroupBy(x => x.ViolationType)
                        .Select(x => (
                            ((int)x.Key).ToString(),
                            x.Key.ToString(),
                            x.Count()))),
                Items = items
            };
        }

        public async Task<List<PenaltyUserReportResponse>> GetPenaltyUsersAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            ValidateTop(top);
            var scope = await GetScopeAsync(cancellationToken);
            var violations = await _repository.GetViolationsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            if (scope.IsAdmin)
            {
                var users = await _repository.GetUsersWithPenaltyPointsAsync(
                    cancellationToken);

                return users
                    .Select(user =>
                    {
                        var userViolations = violations
                            .Where(x => x.UserId == user.UserId)
                            .ToList();

                        return new PenaltyUserReportResponse
                        {
                            UserId = user.UserId,
                            FullName = user.FullName,
                            DepartmentName = user.Department?.DepartmentName ?? string.Empty,
                            PenaltyPoints = user.PenaltyPoints,
                            ActiveViolationCount = userViolations.Count(
                                x => x.Status == ViolationStatus.Active),
                            TotalViolationCount = userViolations.Count,
                            UserStatus = user.Status.ToString(),
                            RestrictionUntil = user.RestrictionUntil
                        };
                    })
                    .OrderByDescending(x => x.PenaltyPoints)
                    .ThenByDescending(x => x.ActiveViolationCount)
                    .ThenBy(x => x.FullName)
                    .Take(top)
                    .ToList();
            }

            return violations
                .GroupBy(x => x.UserId)
                .Select(group =>
                {
                    var user = group.First().User;
                    int scopedActivePoints = group
                        .Where(x => x.Status == ViolationStatus.Active)
                        .Sum(x => x.PenaltyPointsAdded);

                    return new PenaltyUserReportResponse
                    {
                        UserId = group.Key,
                        FullName = user?.FullName ?? string.Empty,
                        DepartmentName = user?.Department?.DepartmentName ?? string.Empty,
                        PenaltyPoints = scopedActivePoints,
                        ActiveViolationCount = group.Count(
                            x => x.Status == ViolationStatus.Active),
                        TotalViolationCount = group.Count(),
                        UserStatus = user?.Status.ToString() ?? string.Empty,
                        RestrictionUntil = user?.RestrictionUntil
                    };
                })
                .OrderByDescending(x => x.PenaltyPoints)
                .ThenByDescending(x => x.ActiveViolationCount)
                .ThenBy(x => x.FullName)
                .Take(top)
                .ToList();
        }

        public async Task<NoShowRateResponse> GetNoShowRateAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(from, to, cancellationToken);
            int noShowCount = bookings.Count(x => x.Status == BookingStatus.NoShow);
            int completedCount = bookings.Count(x => x.Status == BookingStatus.Completed);
            int concluded = noShowCount + completedCount;

            return new NoShowRateResponse
            {
                NoShowCount = noShowCount,
                CompletedCount = completedCount,
                ConcludedBookingCount = concluded,
                NoShowRate = Percentage(noShowCount, concluded)
            };
        }

        public async Task<List<UsageTrendResponse>> GetUsageTrendAsync(
            DateTime from,
            DateTime to,
            string groupBy,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            string normalizedGroup = NormalizeGroupBy(groupBy);
            var scope = await GetScopeAsync(cancellationToken);
            var logs = await _repository.GetUsageLogsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            var results = new List<UsageTrendResponse>();
            DateTime periodStart = GetPeriodStart(from, normalizedGroup);

            while (periodStart < to)
            {
                DateTime rawPeriodEnd = GetPeriodEnd(periodStart, normalizedGroup);
                DateTime effectiveStart = periodStart < from ? from : periodStart;
                DateTime effectiveEnd = rawPeriodEnd > to ? to : rawPeriodEnd;

                int usageCount = logs.Count(log =>
                    log.ActualCheckin >= effectiveStart
                    && log.ActualCheckin < effectiveEnd);

                double usageHours = logs.Sum(log =>
                {
                    DateTime logEnd = ResolveCheckout(log, effectiveEnd);
                    DateTime overlapStart = log.ActualCheckin < effectiveStart
                        ? effectiveStart
                        : log.ActualCheckin;
                    DateTime overlapEnd = logEnd > effectiveEnd
                        ? effectiveEnd
                        : logEnd;

                    return overlapEnd > overlapStart
                        ? (overlapEnd - overlapStart).TotalHours
                        : 0;
                });

                results.Add(new UsageTrendResponse
                {
                    PeriodStart = periodStart,
                    PeriodEnd = rawPeriodEnd,
                    UsageCount = usageCount,
                    TotalUsageHours = Round(usageHours)
                });

                periodStart = rawPeriodEnd;
            }

            return results;
        }

        public async Task<DashboardResponse> GetDashboardAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);

            var statuses = await GetBookingsByStatusAsync(from, to, cancellationToken);
            var purposes = await GetBookingsByPurposeAsync(from, to, cancellationToken);
            var departments = await GetBookingsByDepartmentAsync(from, to, cancellationToken);
            var topLabs = await GetMostUsedLabRoomsAsync(from, to, 5, cancellationToken);
            var topEquipments = await GetMostUsedEquipmentsAsync(from, to, 5, cancellationToken);
            var penalties = await GetPenaltyUsersAsync(from, to, 5, cancellationToken);
            var noShow = await GetNoShowRateAsync(from, to, cancellationToken);
            var usageTrend = await GetUsageTrendAsync(from, to, "day", cancellationToken);

            var scope = await GetScopeAsync(cancellationToken);
            var logs = await _repository.GetUsageLogsAsync(from, to, scope.AllowedLabIds, cancellationToken);
            var maintenances = await _repository.GetMaintenancesAsync(from, to, scope.AllowedLabIds, cancellationToken);
            var violations = await _repository.GetViolationsAsync(from, to, scope.AllowedLabIds, cancellationToken);

            return new DashboardResponse
            {
                From = from,
                To = to,
                TotalBookings = statuses.Sum(x => x.Count),
                TotalUsageLogs = logs.Count,
                TotalViolations = violations.Count,
                TotalMaintenanceCost = maintenances
                    .Where(x => x.Status != MaintenanceStatus.Cancelled)
                    .Sum(x => x.MaintenanceCost),
                NoShow = noShow,
                BookingStatusCounts = statuses,
                BookingPurposeCounts = purposes,
                BookingDepartmentCounts = departments,
                MostUsedLabRooms = topLabs,
                MostUsedEquipments = topEquipments,
                UsersWithMostPenaltyPoints = penalties,
                UsageTrend = usageTrend
            };
        }

        private async Task<IReadOnlyList<Booking>> GetScopedBookingsAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);
            return await _repository.GetBookingsAsync(from, to, scope.AllowedLabIds, cancellationToken);
        }

        private async Task<ReportScope> GetScopeAsync(
            CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var actor = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");

            if (actor.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản không hoạt động.");

            if (actor.Role?.RoleName == RoleName.Admin)
                return new ReportScope(true, null);

            if (actor.Role?.RoleName == RoleName.LabManager)
            {
                var labs = await _labRoomRepository.GetByManagerIdAsync(
                    actor.UserId,
                    cancellationToken);

                return new ReportScope(
                    false,
                    labs.Select(x => x.LabId).ToArray());
            }

            throw new UnauthorizedAccessException(
                "Requester không được xem báo cáo quản trị.");
        }

        private static List<CategoryCountResponse> BuildCategoryCounts(
            IEnumerable<(string Key, string DisplayName, int Count)> source,
            bool includeZero = false)
        {
            var items = source
                .Where(x => includeZero || x.Count > 0)
                .ToList();

            int total = items.Sum(x => x.Count);

            return items.Select(x => new CategoryCountResponse
            {
                Key = x.Key,
                DisplayName = x.DisplayName,
                Count = x.Count,
                Percentage = Percentage(x.Count, total)
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.DisplayName)
            .ToList();
        }

        private static bool IsUtilizedBooking(Booking booking)
        {
            return booking.Status is BookingStatus.Approved or BookingStatus.Completed;
        }

        private static bool BookingTouchesLab(Booking booking, int labId)
        {
            return booking.BookingItems.Any(item =>
                item.LabId == labId
                || item.Equipment?.LabId == labId);
        }

        private static bool UsageLogTouchesLab(UsageLog log, int labId)
        {
            return log.BookingItem?.LabId == labId
                || log.BookingItem?.Equipment?.LabId == labId;
        }

        private static DateTime ResolveCheckout(UsageLog log, DateTime reportTo)
        {
            DateTime resolved = log.ActualCheckout
                ?? (DateTime.UtcNow < reportTo ? DateTime.UtcNow : reportTo);

            return resolved > reportTo ? reportTo : resolved;
        }

        private static double MergeAndMeasure(
            IEnumerable<(DateTime Start, DateTime End)> source,
            DateTime from,
            DateTime to)
        {
            var intervals = source
                .Select(x =>
                {
                    DateTime start = x.Start < from ? from : x.Start;
                    DateTime end = x.End > to ? to : x.End;
                    return (Start: start, End: end);
                })
                .Where(x => x.Start < x.End)
                .OrderBy(x => x.Start)
                .ThenBy(x => x.End)
                .ToList();

            if (intervals.Count == 0)
                return 0;

            DateTime currentStart = intervals[0].Start;
            DateTime currentEnd = intervals[0].End;
            double hours = 0;

            foreach (var interval in intervals.Skip(1))
            {
                if (interval.Start <= currentEnd)
                {
                    if (interval.End > currentEnd)
                        currentEnd = interval.End;
                    continue;
                }

                hours += (currentEnd - currentStart).TotalHours;
                currentStart = interval.Start;
                currentEnd = interval.End;
            }

            hours += (currentEnd - currentStart).TotalHours;
            return hours;
        }

        private static MostUsedResourceResponse ToMostUsed(
            ResourceUtilizationResponse source)
        {
            return new MostUsedResourceResponse
            {
                ResourceType = source.ResourceType,
                ResourceId = source.ResourceId,
                ResourceName = source.ResourceName,
                LabId = source.LabId,
                LabName = source.LabName,
                BookingCount = source.BookingCount,
                ReservedHours = source.ReservedHours,
                UsageCount = source.UsageCount,
                ActualUsageHours = source.ActualUsageHours
            };
        }

        private static string NormalizeGroupBy(string groupBy)
        {
            string normalized = string.IsNullOrWhiteSpace(groupBy)
                ? "day"
                : groupBy.Trim().ToLowerInvariant();

            if (normalized is not "day" and not "week" and not "month")
                throw new ArgumentException("GroupBy chỉ nhận day, week hoặc month.");

            return normalized;
        }

        private static DateTime GetPeriodStart(DateTime value, string groupBy)
        {
            DateTime date = value.Date;

            if (groupBy == "month")
                return new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            if (groupBy == "week")
            {
                int diff = (7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                return DateTime.SpecifyKind(date.AddDays(-diff), DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        private static DateTime GetPeriodEnd(DateTime periodStart, string groupBy)
        {
            return groupBy switch
            {
                "month" => periodStart.AddMonths(1),
                "week" => periodStart.AddDays(7),
                _ => periodStart.AddDays(1)
            };
        }

        private static void ValidateRange(DateTime from, DateTime to)
        {
            if (from >= to)
                throw new ArgumentException("From phải nhỏ hơn To.");

            if (to - from > MaximumRange)
            {
                throw new ArgumentException(
                    "Khoảng thời gian báo cáo không được vượt quá 366 ngày.");
            }
        }

        private static void ValidateTop(int top)
        {
            if (top <= 0 || top > MaximumTop)
                throw new ArgumentException($"Top phải nằm trong khoảng 1 đến {MaximumTop}.");
        }

        private static double Percentage(double numerator, double denominator)
        {
            if (denominator <= 0)
                return 0;

            return Round(Math.Min(100, numerator / denominator * 100));
        }

        private static double Round(double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private sealed record ReportScope(
            bool IsAdmin,
            IReadOnlyCollection<int>? AllowedLabIds);
    }
}
