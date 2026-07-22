using Application.DTOs.Maintenances;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using AutoMapper;

namespace Application.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public MaintenanceService(
            IMapper mapper,
            IMaintenanceRepository repository,
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

        public async Task<List<MaintenanceResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            IReadOnlyList<Maintenance> maintenances =
                actor.Role?.RoleName == RoleName.LabManager
                    ? await _repository.GetByManagerIdAsync(
                        actor.UserId,
                        cancellationToken)
                    : await _repository.GetAllAsync(cancellationToken);

            return maintenances
                .Select(maintenance => _mapper.Map<MaintenanceResponse>(maintenance))
                .ToList();
        }

        public async Task<MaintenanceDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            var maintenance = await _repository.GetDetailAsync(id, cancellationToken);

            if (maintenance is null)
                return null;

            if (actor.Role?.RoleName == RoleName.LabManager
                && !await CanManageResourceAsync(
                    actor.UserId,
                    maintenance.LabId,
                    maintenance.EquipmentId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được xem lịch bảo trì của phòng mình quản lý.");
            }

            return _mapper.Map<MaintenanceDetailResponse>(maintenance);
        }

        public async Task<List<MaintenanceResponse>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);

            var labRoom = await _unitOfWork.LabRooms.GetByIdAsync(labId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {labId}.");

            var maintenances = await _repository.GetByResourceAsync(
                labRoom.LabId,
                null,
                cancellationToken);

            return maintenances
                .Select(maintenance => _mapper.Map<MaintenanceResponse>(maintenance))
                .ToList();
        }

        public async Task<List<MaintenanceResponse>> GetByEquipmentIdAsync(
            int equipmentId,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);

            var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                equipmentId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {equipmentId}.");

            var maintenances = await _repository.GetByResourceAsync(
                null,
                equipment.EquipmentId,
                cancellationToken);

            return maintenances
                .Select(maintenance => _mapper.Map<MaintenanceResponse>(maintenance))
                .ToList();
        }

        public async Task<MaintenanceDetailResponse> CreateAsync(
            CreateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);
            await ValidateResourceAsync(request.LabId, request.EquipmentId, cancellationToken);
            await EnsureCanManageResourceAsync(actor, request.LabId, request.EquipmentId, cancellationToken);

            await ValidateConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                excludeMaintenanceId: null,
                cancellationToken);

            var maintenance = new Maintenance(
                actor.UserId,
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                request.MaintenanceCost,
                request.Notes);

            maintenance.ConfigureRecurrence(
                request.RecurrenceType,
                request.RecurrenceInterval,
                request.RecurrenceEndDate);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    await _repository.AddAsync(maintenance, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    await AddMaintenanceNotificationAsync(
                        maintenance,
                        "được tạo",
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Create,
                        entityName: nameof(Maintenance),
                        entityId: maintenance.MaintenanceId,
                        oldValue: null,
                        newValue: Snapshot(maintenance),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            var created = await _repository.GetDetailAsync(
                maintenance.MaintenanceId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Không thể lấy thông tin lịch bảo trì vừa tạo.");

            return _mapper.Map<MaintenanceDetailResponse>(created);
        }

        public async Task UpdateAsync(
            int id,
            UpdateMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateTime(request.StartTime, request.EndTime, requireFuture: true);

            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);
            var maintenance = await GetMaintenanceOrThrowAsync(id, cancellationToken);

            await EnsureCanManageResourceAsync(
                actor,
                maintenance.LabId,
                maintenance.EquipmentId,
                cancellationToken);

            await ValidateResourceAsync(request.LabId, request.EquipmentId, cancellationToken);
            await EnsureCanManageResourceAsync(
                actor,
                request.LabId,
                request.EquipmentId,
                cancellationToken);

            await ValidateConflictAsync(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                id,
                cancellationToken);

            var oldValue = Snapshot(maintenance);

            maintenance.UpdateDetails(
                request.LabId,
                request.EquipmentId,
                request.StartTime,
                request.EndTime,
                request.MaintenanceCost,
                request.Notes);

            maintenance.ConfigureRecurrence(
                request.RecurrenceType,
                request.RecurrenceInterval,
                request.RecurrenceEndDate,
                maintenance.ParentMaintenanceId);

            _repository.Update(maintenance);
            await AddMaintenanceNotificationAsync(maintenance, "được cập nhật", cancellationToken);

            await _auditLogWriter.WriteAsync(
                actorUserId: actor.UserId,
                actionType: AuditActionType.Update,
                entityName: nameof(Maintenance),
                entityId: maintenance.MaintenanceId,
                oldValue: oldValue,
                newValue: Snapshot(maintenance),
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task StartAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var maintenance = await GetMaintenanceOrThrowAsync(id, ct);
                    await EnsureCanManageResourceAsync(
                        actor,
                        maintenance.LabId,
                        maintenance.EquipmentId,
                        ct);

                    DateTime now = DateTime.UtcNow;

                    if (now < maintenance.StartTime)
                    {
                        throw new InvalidOperationException(
                            "Chưa đến thời gian bắt đầu lịch bảo trì.");
                    }

                    if (now >= maintenance.EndTime)
                    {
                        throw new InvalidOperationException(
                            "Không thể bắt đầu lịch bảo trì đã hết hạn.");
                    }

                    bool resourceIsInUse =
                        await _unitOfWork.UsageLogs.HasOpenLogForResourceAsync(
                            maintenance.LabId,
                            maintenance.EquipmentId,
                            ct);

                    if (resourceIsInUse)
                    {
                        throw new InvalidOperationException(
                            "Không thể bắt đầu bảo trì khi tài nguyên đang có lượt sử dụng chưa checkout.");
                    }

                    var oldValue = Snapshot(maintenance);

                    await SetResourceMaintenanceStatusAsync(maintenance, ct);
                    maintenance.Start();
                    _repository.Update(maintenance);

                    await AddMaintenanceNotificationAsync(
                        maintenance,
                        "đã bắt đầu",
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Maintenance),
                        entityId: maintenance.MaintenanceId,
                        oldValue: oldValue,
                        newValue: Snapshot(maintenance),
                        cancellationToken: ct);

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
                    var maintenance = await GetMaintenanceOrThrowAsync(id, ct);
                    await EnsureCanManageResourceAsync(
                        actor,
                        maintenance.LabId,
                        maintenance.EquipmentId,
                        ct);

                    var oldValue = Snapshot(maintenance);

                    maintenance.Complete();
                    await RestoreResourceStatusAsync(
                        maintenance,
                        completed: true,
                        ct);
                    _repository.Update(maintenance);

                    await AddMaintenanceNotificationAsync(
                        maintenance,
                        "đã hoàn thành",
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Maintenance),
                        entityId: maintenance.MaintenanceId,
                        oldValue: oldValue,
                        newValue: Snapshot(maintenance),
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task CancelAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var maintenance = await GetMaintenanceOrThrowAsync(id, ct);
                    await EnsureCanManageResourceAsync(
                        actor,
                        maintenance.LabId,
                        maintenance.EquipmentId,
                        ct);

                    bool wasInProgress =
                        maintenance.Status == MaintenanceStatus.InProgress;
                    var oldValue = Snapshot(maintenance);

                    maintenance.Cancel();

                    if (wasInProgress)
                    {
                        await RestoreResourceStatusAsync(
                            maintenance,
                            completed: false,
                            ct);
                    }

                    _repository.Update(maintenance);
                    await AddMaintenanceNotificationAsync(
                        maintenance,
                        "đã bị hủy; các kỳ lặp sau vẫn tiếp tục",
                        ct);

                    await _auditLogWriter.WriteAsync(
                        actorUserId: actor.UserId,
                        actionType: AuditActionType.Update,
                        entityName: nameof(Maintenance),
                        entityId: maintenance.MaintenanceId,
                        oldValue: oldValue,
                        newValue: new
                        {
                            Maintenance = Snapshot(maintenance),
                            Action = "CancelOccurrence",
                            RecurrenceContinues =
                                maintenance.RecurrenceType
                                    != MaintenanceRecurrenceType.None
                        },
                        cancellationToken: ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        public async Task CancelSeriesAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentActiveUserAsync(cancellationToken);
            EnsureManagerOrAdmin(actor);

            await _unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var target = await GetMaintenanceOrThrowAsync(id, ct);
                    await EnsureCanManageResourceAsync(
                        actor,
                        target.LabId,
                        target.EquipmentId,
                        ct);

                    var series = await _repository.GetRecurrenceSeriesAsync(
                        id,
                        ct);

                    if (series.Count == 0)
                    {
                        throw new KeyNotFoundException(
                            $"Không tìm thấy chuỗi bảo trì chứa lịch ID {id}.");
                    }

                    foreach (var maintenance in series)
                    {
                        var oldValue = Snapshot(maintenance);
                        bool wasInProgress =
                            maintenance.Status == MaintenanceStatus.InProgress;

                        if (maintenance.Status is MaintenanceStatus.Scheduled
                            or MaintenanceStatus.InProgress)
                        {
                            maintenance.Cancel();

                            if (wasInProgress)
                            {
                                await RestoreResourceStatusAsync(
                                    maintenance,
                                    completed: false,
                                    ct);
                            }
                        }

                        maintenance.StopRecurrence();
                        _repository.Update(maintenance);

                        await _auditLogWriter.WriteAsync(
                            actorUserId: actor.UserId,
                            actionType: AuditActionType.Update,
                            entityName: nameof(Maintenance),
                            entityId: maintenance.MaintenanceId,
                            oldValue: oldValue,
                            newValue: new
                            {
                                Maintenance = Snapshot(maintenance),
                                Action = "CancelRecurringSeries",
                                RequestedFromMaintenanceId = id
                            },
                            cancellationToken: ct);
                    }

                    await AddMaintenanceNotificationAsync(
                        target,
                        "và toàn bộ chuỗi định kỳ đã bị hủy",
                        ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        private async Task SetResourceMaintenanceStatusAsync(
            Maintenance maintenance,
            CancellationToken cancellationToken)
        {
            if (maintenance.LabId.HasValue)
            {
                var lab = await _unitOfWork.LabRooms.GetDetailAsync(
                    maintenance.LabId.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {maintenance.LabId.Value}.");

                maintenance.CapturePreviousResourceStatus((int)lab.Status);
                lab.StartMaintenance();
                _unitOfWork.LabRooms.Update(lab);

                // Booking/maintenance conflict của phòng đã khóa toàn bộ thiết bị
                // trong phòng. Không đổi hàng loạt trạng thái thiết bị để tránh làm
                // mất trạng thái Broken/Unavailable riêng của từng thiết bị.
            }
            else
            {
                var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                    maintenance.EquipmentId!.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {maintenance.EquipmentId.Value}.");

                maintenance.CapturePreviousResourceStatus((int)equipment.Status);
                equipment.StartMaintenance(allowBroken: true);
                _unitOfWork.Equipments.Update(equipment);
            }
        }

        private async Task RestoreResourceStatusAsync(
            Maintenance maintenance,
            bool completed,
            CancellationToken cancellationToken)
        {
            if (maintenance.LabId.HasValue)
            {
                var lab = await _unitOfWork.LabRooms.GetDetailAsync(
                    maintenance.LabId.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy phòng lab có ID {maintenance.LabId.Value}.");

                if (completed)
                {
                    lab.FinishMaintenance();
                }
                else
                {
                    var previousStatus = maintenance.PreviousResourceStatus.HasValue
                        ? (LabRoomStatus)maintenance.PreviousResourceStatus.Value
                        : LabRoomStatus.Unavailable;

                    lab.RestoreAfterCancelledMaintenance(previousStatus);
                }

                _unitOfWork.LabRooms.Update(lab);
            }
            else
            {
                var equipment = await _unitOfWork.Equipments.GetDetailAsync(
                    maintenance.EquipmentId!.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {maintenance.EquipmentId.Value}.");

                if (completed)
                {
                    equipment.FinishMaintenance();
                }
                else
                {
                    var previousStatus = maintenance.PreviousResourceStatus.HasValue
                        ? (EquipmentStatus)maintenance.PreviousResourceStatus.Value
                        : EquipmentStatus.Broken;

                    equipment.RestoreAfterCancelledMaintenance(previousStatus);
                }

                _unitOfWork.Equipments.Update(equipment);
            }
        }

        private async Task AddMaintenanceNotificationAsync(
            Maintenance maintenance,
            string actionText,
            CancellationToken cancellationToken)
        {
            var managerId = await GetResourceManagerIdAsync(
                maintenance.LabId,
                maintenance.EquipmentId,
                cancellationToken);

            if (!managerId.HasValue)
                return;

            var resourceText = maintenance.LabId.HasValue
                ? $"phòng lab ID {maintenance.LabId.Value}"
                : $"thiết bị ID {maintenance.EquipmentId!.Value}";

            await _unitOfWork.Notifications.AddAsync(
                new Notification(
                    managerId.Value,
                    "Cập nhật bảo trì",
                    $"Lịch bảo trì #{maintenance.MaintenanceId} cho {resourceText} {actionText}.",
                    NotificationType.Maintenance),
                cancellationToken);
        }

        private async Task<int?> GetResourceManagerIdAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            var resourceLabId = await GetResourceLabIdAsync(
                labId,
                equipmentId,
                cancellationToken);

            var lab = await _unitOfWork.LabRooms.GetByIdAsync(
                resourceLabId,
                cancellationToken);

            return lab?.ManagerId;
        }

        private async Task<User> GetAuthenticatedUserAsync(
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetRequiredUserId();
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
            if (actor.Role?.RoleName is not RoleName.Admin and not RoleName.LabManager)
                throw new UnauthorizedAccessException(
                    "Chỉ Admin hoặc LabManager được quản lý bảo trì.");
        }

        private async Task EnsureCanManageResourceAsync(
            User actor,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (actor.Role?.RoleName == RoleName.Admin)
                return;

            if (actor.Role?.RoleName != RoleName.LabManager
                || !await CanManageResourceAsync(
                    actor.UserId,
                    labId,
                    equipmentId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "LabManager chỉ được quản lý bảo trì của phòng mình phụ trách.");
            }
        }

        private async Task<bool> CanManageResourceAsync(
            int managerId,
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            var resourceLabId = await GetResourceLabIdAsync(
                labId,
                equipmentId,
                cancellationToken);

            var lab = await _unitOfWork.LabRooms.GetByIdAsync(
                resourceLabId,
                cancellationToken);

            return lab?.ManagerId == managerId;
        }

        private async Task<int> GetResourceLabIdAsync(
            int? labId,
            int? equipmentId,
            CancellationToken cancellationToken)
        {
            if (labId.HasValue)
                return labId.Value;

            var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                equipmentId!.Value,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");

            return equipment.LabId;
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
                var equipment = await _unitOfWork.Equipments.GetByIdAsync(
                    equipmentId!.Value,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy thiết bị có ID {equipmentId.Value}.");

                if (equipment.Status == EquipmentStatus.Retired)
                    throw new InvalidOperationException("Thiết bị đã ngừng sử dụng.");
            }
        }

        private async Task ValidateConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludeMaintenanceId,
            CancellationToken cancellationToken)
        {
            var maintenanceConflict = await _repository.HasMaintenanceConflictAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeMaintenanceId,
                cancellationToken);

            if (maintenanceConflict)
                throw new InvalidOperationException(
                    "Khung giờ này đã có một lịch bảo trì khác.");

            var bookingConflict = await _repository.HasBookingConflictForMaintenanceAsync(
                labId,
                equipmentId,
                startTime,
                endTime,
                excludeBookingId: null,
                includePending: false,
                cancellationToken: cancellationToken);

            if (bookingConflict)
                throw new InvalidOperationException(
                    "Khung giờ bảo trì bị trùng với một booking Approved.");
        }

        private async Task<Maintenance> GetMaintenanceOrThrowAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _repository.GetDetailAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy lịch bảo trì có ID {id}.");
        }

        private static void ValidateTime(
            DateTime startTime,
            DateTime endTime,
            bool requireFuture)
        {
            if (startTime >= endTime)
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            if (requireFuture && startTime <= DateTime.UtcNow)
                throw new ArgumentException("Thời gian bắt đầu phải ở tương lai.");
        }

        private static object Snapshot(Maintenance maintenance)
        {
            return new
            {
                maintenance.MaintenanceId,
                maintenance.CreatedById,
                maintenance.LabId,
                maintenance.EquipmentId,
                maintenance.StartTime,
                maintenance.EndTime,
                maintenance.MaintenanceCost,
                maintenance.Notes,
                Status = maintenance.Status.ToString(),
                RecurrenceType = maintenance.RecurrenceType.ToString(),
                maintenance.RecurrenceInterval,
                maintenance.RecurrenceEndDate,
                maintenance.ParentMaintenanceId,
                maintenance.RecurrenceStopped,
                maintenance.PreviousResourceStatus
            };
        }

    }
}
