using Application.DTOs.Users;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace Application.Services
{
    public class UserManagementService : IUserManagementService
    {
        private const int MaximumPageSize = 100;

        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogWriter _auditLogWriter;

        private readonly IMapper _mapper;

        public UserManagementService(
            IMapper mapper,
            IUserRepository repository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IAuditLogWriter auditLogWriter)
        {
            _mapper = mapper;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _auditLogWriter = auditLogWriter;
        }

        public async Task<PagedUserResponse> SearchAsync(
            string? keyword,
            RoleName? roleName,
            int? departmentId,
            UserStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            await GetCurrentAdminAsync(cancellationToken);

            if (pageNumber <= 0)
            {
                throw new ArgumentException(
                    "PageNumber phải lớn hơn 0.");
            }

            if (pageSize <= 0 || pageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"PageSize phải nằm trong khoảng 1 đến {MaximumPageSize}.");
            }

            if (roleName.HasValue
                && !Enum.IsDefined(roleName.Value))
            {
                throw new ArgumentException(
                    "Role không hợp lệ.");
            }

            if (status.HasValue
                && !Enum.IsDefined(status.Value))
            {
                throw new ArgumentException(
                    "Trạng thái người dùng không hợp lệ.");
            }

            if (departmentId.HasValue)
            {
                _ = await _unitOfWork.Departments.GetByIdAsync(
                        departmentId.Value,
                        cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy khoa/phòng ban có ID {departmentId.Value}.");
            }

            var (items, totalCount) =
                await _repository.SearchAsync(
                    keyword,
                    roleName,
                    departmentId,
                    status,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            return new PagedUserResponse
            {
                Items = items
                    .Select(user => MapUser(user))
                    .ToList(),

                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(
                    totalCount / (double)pageSize)
            };
        }

        public async Task<UserManagementResponse?> GetByIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            await GetCurrentAdminAsync(cancellationToken);

            var user = await _repository.GetUserByIdAsync(
                userId,
                cancellationToken);

            return user is null
                ? null
                : MapUser(user);
        }

        public async Task<UserManagementResponse> UpdateAsync(
            int userId,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateProfileRequest(request);

            var actor = await GetCurrentAdminAsync(
                cancellationToken);

            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            if (await _repository.IsUsernameExistsAsync(
                    request.Username,
                    userId,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "Username đã tồn tại.");
            }

            if (await _repository.IsEmailExistsAsync(
                    request.Email,
                    userId,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "Email đã tồn tại.");
            }

            var oldValue = Snapshot(user);

            user.UpdateProfile(
                request.FullName,
                request.Username,
                request.Email);

            _repository.Update(user);

            await SaveWithAuditAsync(
                actor.UserId,
                user,
                oldValue,
                cancellationToken);

            return await ReloadResponseAsync(
                userId,
                cancellationToken);
        }

        public async Task<UserManagementResponse> ChangeRoleAsync(
            int userId,
            ChangeUserRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await GetCurrentAdminAsync(
                cancellationToken);

            EnsureNotSelf(
                actor.UserId,
                userId,
                "đổi role của chính mình");

            var role = await _unitOfWork.Roles.GetByIdAsync(
                    request.RoleId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy role có ID {request.RoleId}.");

            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            if (user.Role?.RoleName == RoleName.LabManager
                && role.RoleName != RoleName.LabManager)
            {
                var managedLabs =
                    await _unitOfWork.LabRooms.GetByManagerIdAsync(
                        user.UserId,
                        cancellationToken);

                if (managedLabs.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Phải chuyển các phòng lab đang quản lý cho LabManager khác trước khi đổi role.");
                }
            }

            var oldValue = Snapshot(user);

            user.ChangeRole(role.RoleId);
            _repository.Update(user);

            await SaveWithAuditAsync(
                actor.UserId,
                user,
                oldValue,
                cancellationToken);

            return MapUser(
                user,
                roleNameOverride: role.RoleName);
        }

        public async Task<UserManagementResponse> ChangeDepartmentAsync(
            int userId,
            ChangeUserDepartmentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await GetCurrentAdminAsync(
                cancellationToken);

            var department =
                await _unitOfWork.Departments.GetByIdAsync(
                    request.DepartmentId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy khoa/phòng ban có ID {request.DepartmentId}.");

            if (department.Status != DepartmentStatus.Active)
            {
                throw new InvalidOperationException(
                    "Không thể chuyển người dùng vào khoa/phòng ban đã ngừng sử dụng.");
            }

            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            var oldValue = Snapshot(user);

            user.ChangeDepartment(
                department.DepartmentId);

            _repository.Update(user);

            await SaveWithAuditAsync(
                actor.UserId,
                user,
                oldValue,
                cancellationToken);

            return MapUser(
                user,
                departmentNameOverride:
                    department.DepartmentName);
        }

        public Task<UserManagementResponse> LockAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return ChangeStatusCoreAsync(
                userId,
                UserStatus.Locked,
                null,
                preventSelf: true,
                cancellationToken);
        }

        public Task<UserManagementResponse> UnlockAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return ChangeStatusCoreAsync(
                userId,
                UserStatus.Active,
                null,
                preventSelf: false,
                cancellationToken);
        }

        public Task<UserManagementResponse> DeactivateAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return ChangeStatusCoreAsync(
                userId,
                UserStatus.Inactive,
                null,
                preventSelf: true,
                cancellationToken);
        }

        public Task<UserManagementResponse> ActivateAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return ChangeStatusCoreAsync(
                userId,
                UserStatus.Active,
                null,
                preventSelf: false,
                cancellationToken);
        }

        public async Task<UserManagementResponse> SetStatusAsync(
            int userId,
            SetUserStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            bool preventSelf =
                request.Status != UserStatus.Active;

            return await ChangeStatusCoreAsync(
                userId,
                request.Status,
                request.RestrictionUntil,
                preventSelf,
                cancellationToken);
        }

        public async Task<UserPenaltyResponse> GetPenaltyAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            await GetCurrentAdminAsync(
                cancellationToken);

            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            return new UserPenaltyResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                PenaltyPoints = user.PenaltyPoints,
                Status = user.Status,
                RestrictionUntil = user.RestrictionUntil
            };
        }

        private async Task<UserManagementResponse> ChangeStatusCoreAsync(
            int userId,
            UserStatus status,
            DateTime? restrictionUntil,
            bool preventSelf,
            CancellationToken cancellationToken)
        {
            var actor = await GetCurrentAdminAsync(
                cancellationToken);

            if (preventSelf)
            {
                EnsureNotSelf(
                    actor.UserId,
                    userId,
                    "khóa hoặc vô hiệu hóa chính mình");
            }

            var user = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            if (status is UserStatus.Active
                or UserStatus.Restricted)
            {
                var department =
                    await _unitOfWork.Departments.GetByIdAsync(
                        user.DepartmentId,
                        cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Không tìm thấy khoa/phòng ban có ID {user.DepartmentId}.");

                if (department.Status
                    != DepartmentStatus.Active)
                {
                    throw new InvalidOperationException(
                        "Không thể kích hoạt người dùng thuộc khoa/phòng ban đã ngừng sử dụng.");
                }
            }

            var oldValue = Snapshot(user);

            user.SetStatus(
                status,
                restrictionUntil);

            _repository.Update(user);

            if (status is UserStatus.Locked
                or UserStatus.Inactive)
            {
                await _unitOfWork.RefreshTokens
                    .RevokeAllByUserIdAsync(
                        user.UserId,
                        cancellationToken);
            }

            await SaveWithAuditAsync(
                actor.UserId,
                user,
                oldValue,
                cancellationToken);

            return await ReloadResponseAsync(
                userId,
                cancellationToken);
        }

        private async Task<User> GetCurrentAdminAsync(
            CancellationToken cancellationToken)
        {
            int actorId =
                _currentUserService.GetRequiredUserId();

            var actor =
                await _repository.GetUserByIdAsync(
                    actorId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {actorId}.");

            if (actor.Status != UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Tài khoản người thao tác không hoạt động.");
            }

            if (actor.Role?.RoleName != RoleName.Admin)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Admin được quản trị người dùng.");
            }

            return actor;
        }

        private async Task<User> GetUserOrThrowAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            return await _repository.GetUserByIdAsync(
                    userId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");
        }

        private async Task SaveWithAuditAsync(
            int actorId,
            User user,
            object oldValue,
            CancellationToken cancellationToken)
        {
            await _auditLogWriter.WriteAsync(
                actorId,
                AuditActionType.Update,
                nameof(User),
                user.UserId,
                oldValue,
                Snapshot(user),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task<UserManagementResponse> ReloadResponseAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var reloaded = await GetUserOrThrowAsync(
                userId,
                cancellationToken);

            return MapUser(reloaded);
        }

        private static void ValidateProfileRequest(
            UpdateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.FullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Username))
            {
                throw new ArgumentException(
                    "Username không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }
        }

        private static void EnsureNotSelf(
            int actorId,
            int targetUserId,
            string action)
        {
            if (actorId == targetUserId)
            {
                throw new InvalidOperationException(
                    $"Admin không được {action} để tránh tự mất quyền quản trị.");
            }
        }

        private static object Snapshot(
            User user)
        {
            return new
            {
                user.UserId,
                user.FullName,
                user.Username,
                user.Email,
                user.RoleId,
                user.DepartmentId,
                user.PenaltyPoints,
                user.RestrictionUntil,
                Status = user.Status.ToString()
            };
        }

        private UserManagementResponse MapUser(
            User user,
            RoleName? roleNameOverride = null,
            string? departmentNameOverride = null)
        {
            var response =
                _mapper.Map<UserManagementResponse>(user);

            if (roleNameOverride.HasValue)
            {
                response.RoleName =
                    roleNameOverride.Value.ToString();
            }

            if (departmentNameOverride is not null)
            {
                response.DepartmentName =
                    departmentNameOverride;
            }

            return response;
        }
    }
}