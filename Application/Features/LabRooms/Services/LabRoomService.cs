using Application.DTOs.LabRooms;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using AutoMapper;

namespace Application.Services
{
    public class LabRoomService : ILabRoomService
    {
        private const int MaximumPageSize = 100;
        private readonly ILabRoomRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public LabRoomService(
            IMapper mapper,
            ILabRoomRepository repository,
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

        public async Task<PagedLabRoomResponse> SearchAsync(
            LabRoomSearchRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await GetAuthenticatedUserAsync(cancellationToken);
            ValidatePaging(request.PageNumber, request.PageSize);
            if (request.ManagerId is <= 0)
                throw new ArgumentException("ManagerId phải lớn hơn 0.");
            if (request.MinimumCapacity is <= 0)
                throw new ArgumentException("MinimumCapacity phải lớn hơn 0.");
            if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
                throw new ArgumentException("Trạng thái phòng lab không hợp lệ.");

            var (items, total) = await _repository.SearchAsync(
                request.Keyword,
                request.Status,
                request.ManagerId,
                request.MinimumCapacity,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return new PagedLabRoomResponse
            {
                Items = _mapper.Map<List<LabRoomResponse>>(items),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
            };
        }

        public async Task<List<LabRoomResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            return (await _repository.GetAllAsync(cancellationToken))
                .Select(room => _mapper.Map<LabRoomResponse>(room))
                .ToList();
        }

        public async Task<LabRoomDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await GetAuthenticatedUserAsync(cancellationToken);
            var room = await _repository.GetDetailAsync(id, cancellationToken);
            return room is null
                ? null
                : _mapper.Map<LabRoomDetailResponse>(room);
        }

        public async Task<LabRoomDetailResponse> CreateAsync(
            CreateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var manager = await ValidateManagerAsync(request.ManagerId, cancellationToken);
            string roomCode = request.RoomCode.Trim();
            if (await _repository.IsRoomCodeExistsAsync(roomCode, null, cancellationToken))
                throw new InvalidOperationException($"Mã phòng '{roomCode}' đã tồn tại.");

            var room = new LabRoom(
                manager.UserId,
                request.LabName,
                roomCode,
                request.Location,
                request.Capacity,
                request.Description,
                request.ImageUrl,
                request.UsageGuideline);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _repository.AddAsync(room, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _auditLogWriter.WriteAsync(
                    actor.UserId,
                    AuditActionType.Create,
                    nameof(LabRoom),
                    room.LabId,
                    null,
                    Snapshot(room),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            var createdRoom =
                await _repository.GetDetailAsync(
                    room.LabId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Không thể đọc lại phòng vừa tạo.");

            return _mapper.Map<LabRoomDetailResponse>(createdRoom);
        }

        public async Task UpdateAsync(
            int id,
            UpdateLabRoomRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var room = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {id}.");
            if (room.Status == LabRoomStatus.Inactive)
                throw new InvalidOperationException("Không thể cập nhật phòng đã ngừng hoạt động.");

            var oldValue = Snapshot(room);
            room.UpdateDetails(
                request.LabName,
                request.Location,
                request.Capacity,
                request.Description,
                request.ImageUrl,
                request.UsageGuideline);
            _repository.Update(room);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(LabRoom),
                room.LabId,
                oldValue,
                Snapshot(room),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ChangeManagerAsync(
            int id,
            ChangeLabRoomManagerRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var room = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {id}.");
            var manager = await ValidateManagerAsync(request.ManagerId, cancellationToken);
            if (room.ManagerId == manager.UserId)
                return;

            var oldValue = Snapshot(room);
            room.ChangeManager(manager.UserId);
            _repository.Update(room);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Update,
                nameof(LabRoom),
                room.LabId,
                oldValue,
                Snapshot(room),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var actor = await GetCurrentAdminAsync(cancellationToken);
            var room = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng lab có ID {id}.");
            if (room.Status == LabRoomStatus.Inactive)
                return;
            if (await _repository.HasActiveDependenciesAsync(id, DateTime.UtcNow, cancellationToken))
                throw new InvalidOperationException(
                    "Không thể ngừng phòng đang có booking, usage log, maintenance hoặc waitlist hoạt động.");

            var oldValue = Snapshot(room);
            room.Deactivate();
            _repository.Update(room);
            await _auditLogWriter.WriteAsync(
                actor.UserId,
                AuditActionType.Delete,
                nameof(LabRoom),
                room.LabId,
                oldValue,
                Snapshot(room),
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
                throw new UnauthorizedAccessException("Chỉ Admin được quản lý phòng lab.");
            return actor;
        }

        private async Task<User> ValidateManagerAsync(
            int managerId,
            CancellationToken cancellationToken)
        {
            var manager = await _unitOfWork.Users.GetUserByIdAsync(managerId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người quản lý có ID {managerId}.");
            if (manager.Role?.RoleName != RoleName.LabManager)
                throw new ArgumentException("Người được giao phòng phải có role LabManager.");
            if (manager.Status != UserStatus.Active)
                throw new InvalidOperationException("LabManager được giao phòng phải đang Active.");
            return manager;
        }

        private static void ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) throw new ArgumentException("PageNumber phải lớn hơn 0.");
            if (pageSize <= 0 || pageSize > MaximumPageSize)
                throw new ArgumentException($"PageSize phải từ 1 đến {MaximumPageSize}.");
        }

        private static object Snapshot(LabRoom room) => new
        {
            room.LabId,
            room.ManagerId,
            room.LabName,
            room.RoomCode,
            room.Location,
            room.Capacity,
            room.Description,
            room.ImageUrl,
            room.UsageGuideline,
            Status = room.Status.ToString()
        };

    }
}
