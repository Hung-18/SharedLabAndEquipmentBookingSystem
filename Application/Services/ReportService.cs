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
        private const int MaximumPageSize = 100;
        private static readonly TimeSpan MaximumRange =
            TimeSpan.FromDays(366);

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

        public async Task<List<ResourceUtilizationResponse>>
            GetLabUtilizationAsync(
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

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            return BuildLabUtilization(
                labs,
                bookings,
                logs,
                maintenances,
                from,
                to);
        }

        public async Task<List<ResourceUtilizationResponse>>
            GetEquipmentUtilizationAsync(
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

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            return BuildEquipmentUtilization(
                equipments,
                bookings,
                logs,
                maintenances,
                from,
                to);
        }

        public async Task<List<CategoryCountResponse>>
            GetBookingsByDepartmentAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(
                from,
                to,
                cancellationToken);

            return BuildBookingsByDepartment(bookings);
        }

        public async Task<List<DepartmentUtilizationResponse>>
            GetDepartmentUtilizationAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);

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

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            return BuildDepartmentUtilization(
                bookings,
                logs,
                maintenances,
                from,
                to);
        }

        public async Task<List<CategoryCountResponse>>
            GetBookingsByPurposeAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(
                from,
                to,
                cancellationToken);

            return BuildBookingsByPurpose(bookings);
        }

        public async Task<List<CategoryCountResponse>>
            GetBookingsByStatusAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(
                from,
                to,
                cancellationToken);

            return BuildBookingsByStatus(bookings);
        }

        public async Task<List<MaintenanceCostResponse>>
            GetMaintenanceCostsByLabAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);

            var labs = await _repository.GetLabRoomsAsync(
                scope.AllowedLabIds,
                cancellationToken);

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            return BuildMaintenanceCostsByLab(
                labs,
                maintenances,
                from,
                to);
        }

        public async Task<List<MaintenanceCostResponse>>
            GetMaintenanceCostsByEquipmentAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);

            var equipments = await _repository.GetEquipmentsAsync(
                scope.AllowedLabIds,
                cancellationToken);

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            return BuildMaintenanceCostsByEquipment(
                equipments,
                maintenances,
                from,
                to);
        }

        public async Task<PagedMaintenanceHistoryResponse>
            GetMaintenanceHistoryAsync(
                MaintenanceHistoryQueryRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRange(request.From, request.To);
            ValidateMaintenanceHistoryQuery(request);

            var scope = await GetScopeAsync(cancellationToken);

            var result = await _repository.GetMaintenanceHistoryAsync(
                request.From,
                request.To,
                request.Status,
                request.LabId,
                request.EquipmentId,
                request.CreatedById,
                scope.AllowedLabIds,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return new PagedMaintenanceHistoryResponse
            {
                From = request.From,
                To = request.To,
                TotalCost = result.TotalCost,
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(
                    result.TotalCount / (double)request.PageSize),
                Items = result.Items
                    .Select(MapMaintenanceHistoryItem)
                    .ToList()
            };
        }

        public async Task<List<MostUsedResourceResponse>>
            GetMostUsedLabRoomsAsync(
                DateTime from,
                DateTime to,
                int top,
                CancellationToken cancellationToken = default)
        {
            ValidateTop(top);

            var utilization = await GetLabUtilizationAsync(
                from,
                to,
                cancellationToken);

            return BuildMostUsed(utilization, top);
        }

        public async Task<List<MostUsedResourceResponse>>
            GetMostUsedEquipmentsAsync(
                DateTime from,
                DateTime to,
                int top,
                CancellationToken cancellationToken = default)
        {
            ValidateTop(top);

            var utilization = await GetEquipmentUtilizationAsync(
                from,
                to,
                cancellationToken);

            return BuildMostUsed(utilization, top);
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

            return BuildViolationSummary(violations);
        }

        public async Task<List<PenaltyUserReportResponse>>
            GetPenaltyUsersAsync(
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

            IReadOnlyList<User> users;
            if (scope.IsAdmin)
            {
                users = await _repository
                    .GetUsersWithPenaltyPointsAsync(
                        cancellationToken);
            }
            else
            {
                users = violations
                    .Where(x => x.User is not null)
                    .Select(x => x.User!)
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .ToList();
            }

            return BuildPenaltyUsers(
                users,
                violations,
                top);
        }

        public async Task<NoShowRateResponse> GetNoShowRateAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var bookings = await GetScopedBookingsAsync(
                from,
                to,
                cancellationToken);

            return BuildNoShowRate(bookings);
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

            return BuildUsageTrend(
                logs,
                from,
                to,
                normalizedGroup);
        }

        public async Task<DashboardResponse> GetDashboardAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            ValidateRange(from, to);

            // Dashboard chỉ tải mỗi nhóm dữ liệu một lần. Không gọi lại các
            // public report method vì mỗi method đó sẽ tạo các query lặp.
            var scope = await GetScopeAsync(cancellationToken);

            var labs = await _repository.GetLabRoomsAsync(
                scope.AllowedLabIds,
                cancellationToken);

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

            var maintenances = await _repository.GetMaintenancesAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            var violations = await _repository.GetViolationsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);

            IReadOnlyList<User> penaltyUsers;
            if (scope.IsAdmin)
            {
                penaltyUsers = await _repository
                    .GetUsersWithPenaltyPointsAsync(
                        cancellationToken);
            }
            else
            {
                penaltyUsers = violations
                    .Where(x => x.User is not null)
                    .Select(x => x.User!)
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .ToList();
            }

            var labUtilization = BuildLabUtilization(
                labs,
                bookings,
                logs,
                maintenances,
                from,
                to);

            var equipmentUtilization = BuildEquipmentUtilization(
                equipments,
                bookings,
                logs,
                maintenances,
                from,
                to);

            var departmentUtilization =
                BuildDepartmentUtilization(
                    bookings,
                    logs,
                    maintenances,
                    from,
                    to);

            var maintenanceCostsInPeriod = maintenances
                .Where(x => IsMaintenanceCostInPeriod(
                    x,
                    from,
                    to))
                .ToList();

            return new DashboardResponse
            {
                From = from,
                To = to,
                TotalBookings = bookings.Count,
                TotalUsageLogs = logs.Count,
                TotalViolations = violations.Count,
                TotalMaintenanceCost =
                    maintenanceCostsInPeriod.Sum(
                        x => x.MaintenanceCost),
                NoShow = BuildNoShowRate(bookings),
                BookingStatusCounts =
                    BuildBookingsByStatus(bookings),
                BookingPurposeCounts =
                    BuildBookingsByPurpose(bookings),
                BookingDepartmentCounts =
                    BuildBookingsByDepartment(bookings),
                LabUtilization = labUtilization,
                EquipmentUtilization =
                    equipmentUtilization,
                DepartmentUtilization =
                    departmentUtilization,
                MostUsedLabRooms =
                    BuildMostUsed(labUtilization, 5),
                MostUsedEquipments =
                    BuildMostUsed(
                        equipmentUtilization,
                        5),
                UsersWithMostPenaltyPoints =
                    BuildPenaltyUsers(
                        penaltyUsers,
                        violations,
                        5),
                UsageTrend = BuildUsageTrend(
                    logs,
                    from,
                    to,
                    "day")
            };
        }

        private async Task<IReadOnlyList<Booking>>
            GetScopedBookingsAsync(
                DateTime from,
                DateTime to,
                CancellationToken cancellationToken)
        {
            ValidateRange(from, to);
            var scope = await GetScopeAsync(cancellationToken);

            return await _repository.GetBookingsAsync(
                from,
                to,
                scope.AllowedLabIds,
                cancellationToken);
        }

        private async Task<ReportScope> GetScopeAsync(
            CancellationToken cancellationToken)
        {
            int userId =
                _currentUserService.GetRequiredUserId();

            var actor = await _userRepository.GetUserByIdAsync(
                userId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");

            if (actor.Status != UserStatus.Active)
                throw new InvalidOperationException(
                    "Tài khoản không hoạt động.");

            if (actor.Role?.RoleName == RoleName.Admin)
                return new ReportScope(true, null);

            if (actor.Role?.RoleName == RoleName.LabManager)
            {
                var labs =
                    await _labRoomRepository.GetByManagerIdAsync(
                        actor.UserId,
                        cancellationToken);

                return new ReportScope(
                    false,
                    labs.Select(x => x.LabId).ToArray());
            }

            throw new UnauthorizedAccessException(
                "Requester không được xem báo cáo quản trị.");
        }

        private static List<ResourceUtilizationResponse>
            BuildLabUtilization(
                IReadOnlyList<LabRoom> labs,
                IReadOnlyList<Booking> bookings,
                IReadOnlyList<UsageLog> logs,
                IReadOnlyList<Maintenance> maintenances,
                DateTime from,
                DateTime to)
        {
            double totalRangeHours =
                (to - from).TotalHours;

            return labs.Select(lab =>
            {
                var matchingBookings = bookings
                    .Where(IsReservedBooking)
                    .Where(x => BookingTouchesLab(
                        x,
                        lab.LabId))
                    .GroupBy(x => x.BookingId)
                    .Select(x => x.First())
                    .ToList();

                var matchingLogs = logs
                    .Where(x => UsageLogTouchesLab(
                        x,
                        lab.LabId))
                    .GroupBy(x => x.LogId)
                    .Select(x => x.First())
                    .ToList();

                // Chỉ maintenance của cả phòng làm giảm giờ khả dụng
                // của phòng. Maintenance một thiết bị vẫn được hiển thị trên
                // calendar nhưng không đồng nghĩa toàn bộ phòng ngừng hoạt động.
                var blockingMaintenances = maintenances
                    .Where(IsBlockingMaintenance)
                    .Where(x => x.LabId == lab.LabId)
                    .ToList();

                double reservedHours = MergeAndMeasure(
                    matchingBookings.Select(
                        x => (x.StartTime, x.EndTime)),
                    from,
                    to);

                double actualUsageHours = MergeAndMeasure(
                    matchingLogs.Select(
                        x => (
                            x.ActualCheckin,
                            ResolveCheckout(x, to))),
                    from,
                    to);

                double maintenanceHours =
                    MergeAndMeasure(
                        blockingMaintenances.Select(
                            x => (
                                x.StartTime,
                                x.EndTime)),
                        from,
                        to);

                double availableHours = Math.Max(
                    0,
                    totalRangeHours - maintenanceHours);

                return new ResourceUtilizationResponse
                {
                    ResourceType =
                        ResourceType.LabRoom.ToString(),
                    ResourceId = lab.LabId,
                    ResourceName = lab.LabName,
                    LabId = lab.LabId,
                    LabName = lab.LabName,
                    BookingCount =
                        matchingBookings.Count,
                    ReservedHours =
                        Round(reservedHours),
                    UsageCount = matchingLogs.Count,
                    ActualUsageHours =
                        Round(actualUsageHours),
                    AvailableHours =
                        Round(availableHours),
                    UtilizationRate = Percentage(
                        actualUsageHours,
                        availableHours)
                };
            })
            .OrderByDescending(x => x.UtilizationRate)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        private static List<ResourceUtilizationResponse>
            BuildEquipmentUtilization(
                IReadOnlyList<Equipment> equipments,
                IReadOnlyList<Booking> bookings,
                IReadOnlyList<UsageLog> logs,
                IReadOnlyList<Maintenance> maintenances,
                DateTime from,
                DateTime to)
        {
            double totalRangeHours =
                (to - from).TotalHours;

            return equipments.Select(equipment =>
            {
                var matchingBookings = bookings
                    .Where(IsReservedBooking)
                    .Where(x => x.BookingItems.Any(
                        item =>
                            item.EquipmentId
                                == equipment.EquipmentId
                            || item.LabId
                                == equipment.LabId))
                    .GroupBy(x => x.BookingId)
                    .Select(x => x.First())
                    .ToList();

                // ActualUsageHours của thiết bị chỉ tính usage log trực tiếp
                // của thiết bị. Booking cả phòng khóa thiết bị nhưng không đủ
                // căn cứ để khẳng định thiết bị đã thực sự được sử dụng.
                var matchingLogs = logs
                    .Where(x =>
                        x.BookingItem?.EquipmentId
                            == equipment.EquipmentId)
                    .GroupBy(x => x.LogId)
                    .Select(x => x.First())
                    .ToList();

                var blockingMaintenances = maintenances
                    .Where(IsBlockingMaintenance)
                    .Where(x =>
                        x.EquipmentId
                            == equipment.EquipmentId
                        || x.LabId == equipment.LabId)
                    .ToList();

                double reservedHours = MergeAndMeasure(
                    matchingBookings.Select(
                        x => (x.StartTime, x.EndTime)),
                    from,
                    to);

                double actualUsageHours = MergeAndMeasure(
                    matchingLogs.Select(
                        x => (
                            x.ActualCheckin,
                            ResolveCheckout(x, to))),
                    from,
                    to);

                double maintenanceHours =
                    MergeAndMeasure(
                        blockingMaintenances.Select(
                            x => (
                                x.StartTime,
                                x.EndTime)),
                        from,
                        to);

                double availableHours = Math.Max(
                    0,
                    totalRangeHours - maintenanceHours);

                return new ResourceUtilizationResponse
                {
                    ResourceType =
                        ResourceType.Equipment.ToString(),
                    ResourceId = equipment.EquipmentId,
                    ResourceName =
                        equipment.EquipmentName,
                    LabId = equipment.LabId,
                    LabName =
                        equipment.LabRoom?.LabName,
                    BookingCount =
                        matchingBookings.Count,
                    ReservedHours =
                        Round(reservedHours),
                    UsageCount = matchingLogs.Count,
                    ActualUsageHours =
                        Round(actualUsageHours),
                    AvailableHours =
                        Round(availableHours),
                    UtilizationRate = Percentage(
                        actualUsageHours,
                        availableHours)
                };
            })
            .OrderByDescending(x => x.UtilizationRate)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        private static List<DepartmentUtilizationResponse>
            BuildDepartmentUtilization(
                IReadOnlyList<Booking> bookings,
                IReadOnlyList<UsageLog> logs,
                IReadOnlyList<Maintenance> maintenances,
                DateTime from,
                DateTime to)
        {
            double totalRangeHours = (to - from).TotalHours;

            var departmentKeys = bookings
                .Where(x => x.User is not null)
                .Select(x => new
                {
                    Id = x.User!.DepartmentId,
                    Name = x.User.Department?.DepartmentName
                        ?? "Không xác định"
                })
                .Concat(logs
                    .Where(x => x.BookingItem?.Booking?.User is not null)
                    .Select(x => new
                    {
                        Id = x.BookingItem!.Booking!.User!.DepartmentId,
                        Name = x.BookingItem.Booking.User.Department?
                            .DepartmentName
                            ?? "Không xác định"
                    }))
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();

            var results = departmentKeys.Select(department =>
            {
                var departmentBookings = bookings
                    .Where(IsReservedBooking)
                    .Where(x => x.User?.DepartmentId == department.Id)
                    .GroupBy(x => x.BookingId)
                    .Select(x => x.First())
                    .ToList();

                var departmentLogs = logs
                    .Where(x => x.BookingItem?.Booking?.User?.DepartmentId
                        == department.Id)
                    .GroupBy(x => x.LogId)
                    .Select(x => x.First())
                    .ToList();

                // ReservedHours được tính theo resource-hour. Một booking có
                // ba BookingItem trong hai giờ tương đương sáu giờ tài nguyên.
                double reservedHours = departmentBookings.Sum(booking =>
                    booking.BookingItems.Sum(_ =>
                        MeasureOverlap(
                            booking.StartTime,
                            booking.EndTime,
                            from,
                            to)));

                double actualUsageHours = departmentLogs.Sum(x =>
                    MeasureOverlap(
                        x.ActualCheckin,
                        ResolveCheckout(x, to),
                        from,
                        to));

                var labIds = new HashSet<int>();
                var equipmentIds = new HashSet<int>();
                var equipmentLabIds = new Dictionary<int, int>();

                foreach (var item in departmentBookings
                    .SelectMany(x => x.BookingItems))
                {
                    AddDepartmentResource(
                        item,
                        labIds,
                        equipmentIds,
                        equipmentLabIds);
                }

                foreach (var log in departmentLogs)
                {
                    if (log.BookingItem is not null)
                    {
                        AddDepartmentResource(
                            log.BookingItem,
                            labIds,
                            equipmentIds,
                            equipmentLabIds);
                    }
                }

                double availableResourceHours = 0;

                foreach (int labId in labIds)
                {
                    double maintenanceHours = MergeAndMeasure(
                        maintenances
                            .Where(IsBlockingMaintenance)
                            .Where(x => x.LabId == labId)
                            .Select(x => (x.StartTime, x.EndTime)),
                        from,
                        to);

                    availableResourceHours += Math.Max(
                        0,
                        totalRangeHours - maintenanceHours);
                }

                foreach (int equipmentId in equipmentIds)
                {
                    equipmentLabIds.TryGetValue(
                        equipmentId,
                        out int equipmentLabId);

                    double maintenanceHours = MergeAndMeasure(
                        maintenances
                            .Where(IsBlockingMaintenance)
                            .Where(x =>
                                x.EquipmentId == equipmentId
                                || (equipmentLabId > 0
                                    && x.LabId == equipmentLabId))
                            .Select(x => (x.StartTime, x.EndTime)),
                        from,
                        to);

                    availableResourceHours += Math.Max(
                        0,
                        totalRangeHours - maintenanceHours);
                }

                return new DepartmentUtilizationResponse
                {
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    BookingCount = departmentBookings.Count,
                    ReservedHours = Round(reservedHours),
                    UsageCount = departmentLogs.Count,
                    ActualUsageHours = Round(actualUsageHours),
                    AvailableResourceHours = Round(availableResourceHours),
                    UtilizationRate = Percentage(
                        actualUsageHours,
                        availableResourceHours)
                };
            }).ToList();

            double totalActualHours =
                results.Sum(x => x.ActualUsageHours);

            foreach (var result in results)
            {
                result.UsageSharePercentage = Percentage(
                    result.ActualUsageHours,
                    totalActualHours);
            }

            return results
                .OrderByDescending(x => x.UtilizationRate)
                .ThenByDescending(x => x.ActualUsageHours)
                .ThenBy(x => x.DepartmentName)
                .ToList();
        }

        private static void AddDepartmentResource(
            BookingItem item,
            ISet<int> labIds,
            ISet<int> equipmentIds,
            IDictionary<int, int> equipmentLabIds)
        {
            if (item.LabId.HasValue)
            {
                labIds.Add(item.LabId.Value);
                return;
            }

            if (!item.EquipmentId.HasValue)
                return;

            int equipmentId = item.EquipmentId.Value;
            equipmentIds.Add(equipmentId);

            if (item.Equipment?.LabId is int labId)
                equipmentLabIds[equipmentId] = labId;
        }

        private static List<CategoryCountResponse>
            BuildBookingsByDepartment(
                IReadOnlyList<Booking> bookings)
        {
            return BuildCategoryCounts(
                bookings.GroupBy(x => new
                {
                    Id = x.User?.DepartmentId ?? 0,
                    Name = x.User?.Department?
                        .DepartmentName
                        ?? "Không xác định"
                })
                .Select(x => (
                    x.Key.Id.ToString(),
                    x.Key.Name,
                    x.Count())));
        }

        private static List<CategoryCountResponse>
            BuildBookingsByPurpose(
                IReadOnlyList<Booking> bookings)
        {
            return BuildCategoryCounts(
                bookings.GroupBy(x => x.PurposeType)
                    .Select(x => (
                        ((int)x.Key).ToString(),
                        x.Key.ToString(),
                        x.Count())));
        }

        private static List<CategoryCountResponse>
            BuildBookingsByStatus(
                IReadOnlyList<Booking> bookings)
        {
            var counts = Enum.GetValues<BookingStatus>()
                .Select(status =>
                {
                    int count = bookings.Count(
                        x => x.Status == status);

                    return (
                        ((int)status).ToString(),
                        status.ToString(),
                        count);
                });

            return BuildCategoryCounts(
                counts,
                includeZero: true);
        }

        private static List<MaintenanceCostResponse>
            BuildMaintenanceCostsByLab(
                IReadOnlyList<LabRoom> labs,
                IReadOnlyList<Maintenance> maintenances,
                DateTime from,
                DateTime to)
        {
            return labs.Select(lab =>
            {
                var matching = maintenances
                    .Where(x => IsMaintenanceCostInPeriod(
                        x,
                        from,
                        to))
                    .Where(x =>
                        x.LabId == lab.LabId
                        || x.Equipment?.LabId
                            == lab.LabId)
                    .ToList();

                return new MaintenanceCostResponse
                {
                    ResourceType =
                        ResourceType.LabRoom.ToString(),
                    ResourceId = lab.LabId,
                    ResourceName = lab.LabName,
                    LabId = lab.LabId,
                    LabName = lab.LabName,
                    MaintenanceCount = matching.Count,
                    TotalCost = matching.Sum(
                        x => x.MaintenanceCost)
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        private static List<MaintenanceCostResponse>
            BuildMaintenanceCostsByEquipment(
                IReadOnlyList<Equipment> equipments,
                IReadOnlyList<Maintenance> maintenances,
                DateTime from,
                DateTime to)
        {
            return equipments.Select(equipment =>
            {
                var matching = maintenances
                    .Where(x => IsMaintenanceCostInPeriod(
                        x,
                        from,
                        to))
                    .Where(x =>
                        x.EquipmentId
                            == equipment.EquipmentId)
                    .ToList();

                return new MaintenanceCostResponse
                {
                    ResourceType =
                        ResourceType.Equipment.ToString(),
                    ResourceId = equipment.EquipmentId,
                    ResourceName =
                        equipment.EquipmentName,
                    LabId = equipment.LabId,
                    LabName =
                        equipment.LabRoom?.LabName,
                    MaintenanceCount = matching.Count,
                    TotalCost = matching.Sum(
                        x => x.MaintenanceCost)
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .ThenBy(x => x.ResourceName)
            .ToList();
        }

        private static List<MostUsedResourceResponse>
            BuildMostUsed(
                IReadOnlyList<ResourceUtilizationResponse>
                    utilization,
                int top)
        {
            return utilization
                .Where(x =>
                    x.UsageCount > 0
                    || x.BookingCount > 0)
                .OrderByDescending(x => x.UsageCount)
                .ThenByDescending(
                    x => x.ActualUsageHours)
                .ThenByDescending(x => x.BookingCount)
                .ThenBy(x => x.ResourceName)
                .Take(top)
                .Select(ToMostUsed)
                .ToList();
        }

        private static ViolationSummaryResponse
            BuildViolationSummary(
                IReadOnlyList<Violation> violations)
        {
            var items = violations.Select(x =>
                new ViolationReportResponse
                {
                    ViolationId = x.ViolationId,
                    UserId = x.UserId,
                    UserName =
                        x.User?.FullName ?? string.Empty,
                    DepartmentName =
                        x.User?.Department?
                            .DepartmentName
                        ?? string.Empty,
                    BookingId = x.BookingId,
                    ViolationType =
                        x.ViolationType.ToString(),
                    PenaltyPointsAdded =
                        x.PenaltyPointsAdded,
                    Status = x.Status.ToString(),
                    LoggedAt = x.LoggedAt
                })
                .ToList();

            return new ViolationSummaryResponse
            {
                TotalCount = items.Count,
                ActiveCount = violations.Count(
                    x => x.Status
                        == ViolationStatus.Active),
                ResolvedCount = violations.Count(
                    x => x.Status
                        == ViolationStatus.Resolved),
                CancelledCount = violations.Count(
                    x => x.Status
                        == ViolationStatus.Cancelled),
                ViolationTypeCounts =
                    BuildCategoryCounts(
                        violations
                            .GroupBy(x =>
                                x.ViolationType)
                            .Select(x => (
                                ((int)x.Key).ToString(),
                                x.Key.ToString(),
                                x.Count()))),
                Items = items
            };
        }

        private static List<PenaltyUserReportResponse>
            BuildPenaltyUsers(
                IReadOnlyList<User> users,
                IReadOnlyList<Violation> violations,
                int top)
        {
            return users
                .GroupBy(x => x.UserId)
                .Select(group =>
                {
                    var user = group.First();
                    var userViolations = violations
                        .Where(x =>
                            x.UserId == user.UserId)
                        .ToList();

                    int periodPoints = userViolations
                        .Where(x =>
                            x.Status
                                != ViolationStatus.Cancelled)
                        .Sum(x =>
                            x.PenaltyPointsAdded);

                    return new PenaltyUserReportResponse
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        DepartmentName =
                            user.Department?
                                .DepartmentName
                            ?? string.Empty,
                        // PenaltyPoints luôn mang cùng ý nghĩa:
                        // tổng điểm hiện tại trên User, không phụ
                        // thuộc vai trò người xem.
                        PenaltyPoints =
                            user.PenaltyPoints,
                        PenaltyPointsInPeriod =
                            periodPoints,
                        ActiveViolationCount =
                            userViolations.Count(
                                x => x.Status
                                    == ViolationStatus.Active),
                        TotalViolationCount =
                            userViolations.Count,
                        UserStatus =
                            user.Status.ToString(),
                        RestrictionUntil =
                            user.RestrictionUntil
                    };
                })
                .Where(x =>
                    x.PenaltyPoints > 0
                    || x.PenaltyPointsInPeriod > 0
                    || x.TotalViolationCount > 0)
                .OrderByDescending(
                    x => x.PenaltyPoints)
                .ThenByDescending(
                    x => x.PenaltyPointsInPeriod)
                .ThenByDescending(
                    x => x.ActiveViolationCount)
                .ThenBy(x => x.FullName)
                .Take(top)
                .ToList();
        }

        private static NoShowRateResponse BuildNoShowRate(
            IReadOnlyList<Booking> bookings)
        {
            int noShowCount = bookings.Count(
                x => x.Status == BookingStatus.NoShow);

            int completedCount = bookings.Count(
                x => x.Status
                    == BookingStatus.Completed);

            int concluded =
                noShowCount + completedCount;

            return new NoShowRateResponse
            {
                NoShowCount = noShowCount,
                CompletedCount = completedCount,
                ConcludedBookingCount = concluded,
                NoShowRate = Percentage(
                    noShowCount,
                    concluded)
            };
        }

        private static List<UsageTrendResponse>
            BuildUsageTrend(
                IReadOnlyList<UsageLog> logs,
                DateTime from,
                DateTime to,
                string normalizedGroup)
        {
            var results =
                new List<UsageTrendResponse>();

            DateTime periodStart =
                GetPeriodStart(
                    from,
                    normalizedGroup);

            while (periodStart < to)
            {
                DateTime rawPeriodEnd =
                    GetPeriodEnd(
                        periodStart,
                        normalizedGroup);

                DateTime effectiveStart =
                    periodStart < from
                        ? from
                        : periodStart;

                DateTime effectiveEnd =
                    rawPeriodEnd > to
                        ? to
                        : rawPeriodEnd;

                // UsageCount dùng cùng quy tắc overlap với TotalUsageHours:
                // một phiên kéo dài qua nhiều kỳ được tính là đang hoạt động
                // trong từng kỳ mà nó thực sự giao nhau.
                int usageCount = logs.Count(log =>
                    log.ActualCheckin < effectiveEnd
                    && ResolveCheckout(log, effectiveEnd) > effectiveStart);

                double usageHours = logs.Sum(log =>
                    MeasureOverlap(
                        log.ActualCheckin,
                        ResolveCheckout(
                            log,
                            effectiveEnd),
                        effectiveStart,
                        effectiveEnd));

                results.Add(new UsageTrendResponse
                {
                    PeriodStart = periodStart,
                    PeriodEnd = rawPeriodEnd,
                    UsageCount = usageCount,
                    TotalUsageHours =
                        Round(usageHours)
                });

                periodStart = rawPeriodEnd;
            }

            return results;
        }

        private static List<CategoryCountResponse>
            BuildCategoryCounts(
                IEnumerable<(
                    string Key,
                    string DisplayName,
                    int Count)> source,
                bool includeZero = false)
        {
            var items = source
                .Where(x =>
                    includeZero || x.Count > 0)
                .ToList();

            int total = items.Sum(x => x.Count);

            return items.Select(x =>
                new CategoryCountResponse
                {
                    Key = x.Key,
                    DisplayName = x.DisplayName,
                    Count = x.Count,
                    Percentage = Percentage(
                        x.Count,
                        total)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DisplayName)
                .ToList();
        }

        private static bool IsReservedBooking(
            Booking booking)
        {
            return booking.Status
                is BookingStatus.Approved
                or BookingStatus.Completed
                or BookingStatus.NoShow;
        }

        private static bool IsBlockingMaintenance(
            Maintenance maintenance)
        {
            return maintenance.Status
                is MaintenanceStatus.Scheduled
                or MaintenanceStatus.InProgress;
        }

        private static bool IsMaintenanceCostInPeriod(
            Maintenance maintenance,
            DateTime from,
            DateTime to)
        {
            // Chi phí được ghi nhận theo ngày bắt đầu để không bị
            // cộng lặp toàn bộ chi phí ở nhiều kỳ báo cáo.
            return maintenance.Status
                    != MaintenanceStatus.Cancelled
                && maintenance.StartTime >= from
                && maintenance.StartTime < to;
        }

        private static bool BookingTouchesLab(
            Booking booking,
            int labId)
        {
            return booking.BookingItems.Any(item =>
                item.LabId == labId
                || item.Equipment?.LabId == labId);
        }

        private static bool UsageLogTouchesLab(
            UsageLog log,
            int labId)
        {
            return log.BookingItem?.LabId == labId
                || log.BookingItem?.Equipment?.LabId
                    == labId;
        }

        private static DateTime ResolveCheckout(
            UsageLog log,
            DateTime reportTo)
        {
            DateTime resolved = log.ActualCheckout
                ?? (DateTime.UtcNow < reportTo
                    ? DateTime.UtcNow
                    : reportTo);

            return resolved > reportTo
                ? reportTo
                : resolved;
        }

        private static double MeasureOverlap(
            DateTime start,
            DateTime end,
            DateTime from,
            DateTime to)
        {
            DateTime overlapStart =
                start < from ? from : start;

            DateTime overlapEnd =
                end > to ? to : end;

            return overlapEnd > overlapStart
                ? (overlapEnd - overlapStart)
                    .TotalHours
                : 0;
        }

        private static double MergeAndMeasure(
            IEnumerable<(DateTime Start, DateTime End)>
                source,
            DateTime from,
            DateTime to)
        {
            var intervals = source
                .Select(x => (
                    Start: x.Start < from
                        ? from
                        : x.Start,
                    End: x.End > to
                        ? to
                        : x.End))
                .Where(x => x.Start < x.End)
                .OrderBy(x => x.Start)
                .ThenBy(x => x.End)
                .ToList();

            if (intervals.Count == 0)
                return 0;

            DateTime currentStart =
                intervals[0].Start;
            DateTime currentEnd =
                intervals[0].End;
            double hours = 0;

            foreach (var interval
                in intervals.Skip(1))
            {
                if (interval.Start <= currentEnd)
                {
                    if (interval.End > currentEnd)
                        currentEnd = interval.End;

                    continue;
                }

                hours += (currentEnd - currentStart)
                    .TotalHours;

                currentStart = interval.Start;
                currentEnd = interval.End;
            }

            hours += (currentEnd - currentStart)
                .TotalHours;

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
                ActualUsageHours =
                    source.ActualUsageHours
            };
        }

        private static string NormalizeGroupBy(
            string groupBy)
        {
            string normalized =
                string.IsNullOrWhiteSpace(groupBy)
                    ? "day"
                    : groupBy.Trim()
                        .ToLowerInvariant();

            if (normalized is not "day"
                and not "week"
                and not "month")
            {
                throw new ArgumentException(
                    "GroupBy chỉ nhận day, week hoặc month.");
            }

            return normalized;
        }

        private static DateTime GetPeriodStart(
            DateTime value,
            string groupBy)
        {
            DateTime date = value.Date;

            if (groupBy == "month")
            {
                return new DateTime(
                    date.Year,
                    date.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);
            }

            if (groupBy == "week")
            {
                int diff =
                    (7 + ((int)date.DayOfWeek
                        - (int)DayOfWeek.Monday))
                    % 7;

                return DateTime.SpecifyKind(
                    date.AddDays(-diff),
                    DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(
                date,
                DateTimeKind.Utc);
        }

        private static DateTime GetPeriodEnd(
            DateTime periodStart,
            string groupBy)
        {
            return groupBy switch
            {
                "month" => periodStart.AddMonths(1),
                "week" => periodStart.AddDays(7),
                _ => periodStart.AddDays(1)
            };
        }

        private static MaintenanceHistoryItemResponse
            MapMaintenanceHistoryItem(Maintenance maintenance)
        {
            bool isLabMaintenance = maintenance.LabId.HasValue;

            return new MaintenanceHistoryItemResponse
            {
                MaintenanceId = maintenance.MaintenanceId,
                ResourceType = isLabMaintenance
                    ? ResourceType.LabRoom.ToString()
                    : ResourceType.Equipment.ToString(),
                ResourceId = maintenance.LabId
                    ?? maintenance.EquipmentId
                    ?? 0,
                ResourceName = isLabMaintenance
                    ? maintenance.LabRoom?.LabName
                        ?? string.Empty
                    : maintenance.Equipment?.EquipmentName
                        ?? string.Empty,
                LabId = maintenance.LabId
                    ?? maintenance.Equipment?.LabId,
                LabName = maintenance.LabRoom?.LabName
                    ?? maintenance.Equipment?.LabRoom?.LabName,
                CreatedById = maintenance.CreatedById,
                CreatedByName = maintenance.CreatedBy?.FullName
                    ?? string.Empty,
                StartTime = maintenance.StartTime,
                EndTime = maintenance.EndTime,
                DurationHours = Round(
                    (maintenance.EndTime
                        - maintenance.StartTime).TotalHours),
                MaintenanceCost = maintenance.MaintenanceCost,
                Notes = maintenance.Notes,
                Status = maintenance.Status.ToString(),
                RecurrenceType =
                    maintenance.RecurrenceType.ToString(),
                RecurrenceInterval =
                    maintenance.RecurrenceInterval,
                RecurrenceEndDate =
                    maintenance.RecurrenceEndDate,
                ParentMaintenanceId =
                    maintenance.ParentMaintenanceId
            };
        }

        private static void ValidateMaintenanceHistoryQuery(
            MaintenanceHistoryQueryRequest request)
        {
            if (request.Status.HasValue
                && !Enum.IsDefined(request.Status.Value))
            {
                throw new ArgumentException(
                    "Trạng thái maintenance không hợp lệ.");
            }

            if (request.LabId.HasValue
                && request.LabId.Value <= 0)
            {
                throw new ArgumentException(
                    "LabId phải lớn hơn 0.");
            }

            if (request.EquipmentId.HasValue
                && request.EquipmentId.Value <= 0)
            {
                throw new ArgumentException(
                    "EquipmentId phải lớn hơn 0.");
            }

            if (request.CreatedById.HasValue
                && request.CreatedById.Value <= 0)
            {
                throw new ArgumentException(
                    "CreatedById phải lớn hơn 0.");
            }

            if (request.PageNumber <= 0)
            {
                throw new ArgumentException(
                    "PageNumber phải lớn hơn 0.");
            }

            if (request.PageSize <= 0
                || request.PageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"PageSize phải nằm trong khoảng 1 đến {MaximumPageSize}.");
            }
        }

        private static void ValidateRange(
            DateTime from,
            DateTime to)
        {
            if (from >= to)
                throw new ArgumentException(
                    "From phải nhỏ hơn To.");

            if (to - from > MaximumRange)
            {
                throw new ArgumentException(
                    "Khoảng thời gian báo cáo không được vượt quá 366 ngày.");
            }
        }

        private static void ValidateTop(int top)
        {
            if (top <= 0 || top > MaximumTop)
            {
                throw new ArgumentException(
                    $"Top phải nằm trong khoảng 1 đến {MaximumTop}.");
            }
        }

        private static double Percentage(
            double numerator,
            double denominator)
        {
            if (denominator <= 0)
                return 0;

            return Round(
                Math.Min(
                    100,
                    numerator / denominator * 100));
        }

        private static double Round(double value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private sealed record ReportScope(
            bool IsAdmin,
            IReadOnlyCollection<int>? AllowedLabIds);
    }
}
