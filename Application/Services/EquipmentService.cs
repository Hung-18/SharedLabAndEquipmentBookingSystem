using Application.DTOs.Equipments;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class EquipmentService : IEquipmentService
    {
        private const int MaximumPageSize = 100;
        private readonly IEquipmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        public EquipmentService(
            IEquipmentRepository repository,
            IUnitOfWork unitOfWork,
            IAuditLogWriter auditLogWriter,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditLogWriter = auditLogWriter;
            _currentUserService = currentUserService;
        }

        public async Task<PagedEquipmentResponse> SearchAsync(
            EquipmentSearchRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await GetAuthenticatedUserAsync(cancellationToken);
            ValidatePaging(request.PageNumber, request.PageSize);
            if (request.LabId is <= 0) throw new ArgumentException("LabId phải lớn hơn 0.");
            if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
                throw new ArgumentException("Trạng thái thiết bị không hợp lệ.");

            var (items, total) = await _repository.SearchAsync(
                request.Keyword,
                request.LabId,
                request.Status,
                request.PageNumber,
                request.PageSize,
                cancellationToken);
            return new PagedEquipmentResponse
            {
                Items = items.Select(MapResponse).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
            };
        }

        public async Task<List<EquipmentResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            return (await _repository.GetAllAsync(cancellationToken))
                .Select(MapResponse)
                .ToList();
        }

        public async Task<EquipmentDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            var equipment = await _repository.GetDetailAsync(id, cancellationToken);
            return equipment is null ? null : MapDetailResponse(equipment);
        }

        public async Task<List<EquipmentResponse>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            _ = await _unitOfWork.LabRooms.GetByIdAsync(labId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {labId}.");
            return (await _repository.GetByLabIdAsync(labId, cancellationToken))
                .Select(MapResponse)
                .ToList();
        }

        public async Task<EquipmentDetailResponse> CreateAsync(
            CreateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            await ValidateTargetLabAsync(request.LabId, cancellationToken);
            var equipment = new Equipment(
                request.LabId,
                request.EquipmentName,
                request.ModelSpecs,
                request.ImageUrl,
                request.UsageGuideline);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _repository.AddAsync(equipment, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _auditLogWriter.WriteAsync(
                    actor.UserId,
                    AuditActionType.Create,
                    nameof(Equipment),
                    equipment.EquipmentId,
                    null,
                    Snapshot(equipment),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return MapDetailResponse(
                await _repository.GetDetailAsync(equipment.EquipmentId, cancellationToken)
                ?? throw new InvalidOperationException("Không thể đọc lại thiết bị vừa tạo."));
        }

        public async Task UpdateAsync(
            int id,
            UpdateEquipmentRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var equipment = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy thiết bị có ID {id}.");
            if (equipment.Status == EquipmentStatus.Retired)
                throw new InvalidOperationException("Không thể cập nhật thiết bị đã Retired.");
            await ValidateTargetLabAsync(request.LabId, cancellationToken);
            if (equipment.LabId != request.LabId
                && await _repository.HasActiveDependenciesAsync(id, DateTime.UtcNow, cancellationToken))
                throw new InvalidOperationException(
                    "Không thể chuyển phòng cho thiết bị đang có booking, usage log, maintenance hoặc waitlist hoạt động.");

            var oldValue = Snapshot(equipment);
            equipment.UpdateDetails(
                request.LabId,
                request.EquipmentName,
                request.ModelSpecs,
                request.ImageUrl,
                request.UsageGuideline);
            _repository.Update(equipment);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(Equipment),
                equipment.EquipmentId,
                oldValue,
                Snapshot(equipment),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var equipment = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy thiết bị có ID {id}.");
            if (equipment.Status == EquipmentStatus.Retired)
                return;
            if (await _repository.HasActiveDependenciesAsync(id, DateTime.UtcNow, cancellationToken))
                throw new InvalidOperationException(
                    "Không thể ngừng thiết bị đang có booking, usage log, maintenance hoặc waitlist hoạt động.");

            var oldValue = Snapshot(equipment);
            equipment.Retire();
            _repository.Update(equipment);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Delete,
                nameof(Equipment),
                equipment.EquipmentId,
                oldValue,
                Snapshot(equipment),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetAuthenticatedUserAsync(CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy người dùng có ID {userId}.");
            if (user.Status is UserStatus.Inactive or UserStatus.Locked)
                throw new InvalidOperationException("Tài khoản không được phép thao tác.");
            return user;
        }

        private async Task<User> GetCurrentAdminAsync(CancellationToken cancellationToken)
        {
            var actor = await GetAuthenticatedUserAsync(cancellationToken);
            if (actor.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản Admin phải ở trạng thái Active.");
            if (actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException("Chỉ Admin được quản lý thiết bị.");
            return actor;
        }

        private async Task ValidateTargetLabAsync(int labId, CancellationToken cancellationToken)
        {
            var lab = await _unitOfWork.LabRooms.GetByIdAsync(labId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {labId}.");
            if (lab.Status == LabRoomStatus.Inactive)
                throw new InvalidOperationException("Không thể đặt thiết bị vào phòng đã ngừng hoạt động.");
        }

        private static void ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) throw new ArgumentException("PageNumber phải lớn hơn 0.");
            if (pageSize <= 0 || pageSize > MaximumPageSize)
                throw new ArgumentException($"PageSize phải từ 1 đến {MaximumPageSize}.");
        }

        private static object Snapshot(Equipment equipment) => new
        {
            equipment.EquipmentId,
            equipment.LabId,
            equipment.EquipmentName,
            equipment.ModelSpecs,
            equipment.ImageUrl,
            equipment.UsageGuideline,
            Status = equipment.Status.ToString()
        };

        private static EquipmentResponse MapResponse(Equipment equipment) => new()
        {
            EquipmentId = equipment.EquipmentId,
            LabId = equipment.LabId,
            EquipmentName = equipment.EquipmentName,
            Status = equipment.Status.ToString()
        };

        private static EquipmentDetailResponse MapDetailResponse(Equipment equipment) => new()
        {
            EquipmentId = equipment.EquipmentId,
            LabId = equipment.LabId,
            EquipmentName = equipment.EquipmentName,
            ModelSpecs = equipment.ModelSpecs,
            ImageUrl = equipment.ImageUrl,
            UsageGuideline = equipment.UsageGuideline,
            Status = equipment.Status.ToString()
        };
    }
}
