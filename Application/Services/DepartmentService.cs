using Application.DTOs.Departments;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogWriter _auditLogWriter;

        public DepartmentService(
            IDepartmentRepository repository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IAuditLogWriter auditLogWriter)
        {
            _repository = repository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _auditLogWriter = auditLogWriter;
        }

        public async Task<List<DepartmentResponse>> GetAllAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default)
        {
            var actor = await EnsureAuthenticatedActiveAsync(cancellationToken);
            bool showActiveOnly = activeOnly
                || actor.Role?.RoleName != RoleName.Admin;

            var departments = showActiveOnly
                ? await _repository.GetActiveDepartmentsAsync(cancellationToken)
                : await _repository.GetAllOrderedAsync(cancellationToken);

            return departments.Select(Map).ToList();
        }

        public async Task<DepartmentResponse?> GetByIdAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var actor = await EnsureAuthenticatedActiveAsync(cancellationToken);
            var department = await _repository.GetByIdAsync(
                departmentId,
                cancellationToken);

            if (department is null)
                return null;

            if (department.Status == DepartmentStatus.Inactive
                && actor.Role?.RoleName != RoleName.Admin)
            {
                return null;
            }

            return Map(department);
        }

        public async Task<DepartmentResponse> CreateAsync(
            CreateDepartmentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateDepartmentName(request.DepartmentName);
            var actor = await EnsureAdminAsync(cancellationToken);

            if (await _repository.IsDepartmentNameExistsAsync(
                    request.DepartmentName,
                    cancellationToken: cancellationToken))
            {
                throw new InvalidOperationException("Tên khoa/phòng ban đã tồn tại.");
            }

            var department = new Department(
                request.DepartmentName,
                request.Description);

            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    await _repository.AddAsync(department, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    await _auditLogWriter.WriteAsync(
                        actor.UserId,
                        AuditActionType.Create,
                        nameof(Department),
                        department.DepartmentId,
                        null,
                        Snapshot(department),
                        ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);

            return Map(department);
        }

        public async Task<DepartmentResponse> UpdateAsync(
            int departmentId,
            UpdateDepartmentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateDepartmentName(request.DepartmentName);
            var actor = await EnsureAdminAsync(cancellationToken);
            var department = await GetOrThrowAsync(departmentId, cancellationToken);

            if (await _repository.IsDepartmentNameExistsAsync(
                    request.DepartmentName,
                    departmentId,
                    cancellationToken))
            {
                throw new InvalidOperationException("Tên khoa/phòng ban đã tồn tại.");
            }

            var oldValue = Snapshot(department);
            department.UpdateDetails(request.DepartmentName, request.Description);
            _repository.Update(department);

            await SaveWithAuditAsync(actor.UserId, department, oldValue, cancellationToken);
            return Map(department);
        }

        public async Task DeactivateAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var actor = await EnsureAdminAsync(cancellationToken);
            var department = await GetOrThrowAsync(departmentId, cancellationToken);

            var activeUsers = await _userRepository.GetByDepartmentAsync(
                departmentId,
                cancellationToken);

            if (activeUsers.Any(x => x.Status == UserStatus.Active
                || x.Status == UserStatus.Restricted))
            {
                throw new InvalidOperationException(
                    "Không thể ngừng sử dụng khoa/phòng ban khi vẫn còn người dùng đang hoạt động.");
            }

            var oldValue = Snapshot(department);
            department.Deactivate();
            _repository.Update(department);

            await SaveWithAuditAsync(actor.UserId, department, oldValue, cancellationToken);
        }

        public async Task<DepartmentResponse> ActivateAsync(
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var actor = await EnsureAdminAsync(cancellationToken);
            var department = await GetOrThrowAsync(departmentId, cancellationToken);
            var oldValue = Snapshot(department);
            department.Activate();
            _repository.Update(department);

            await SaveWithAuditAsync(actor.UserId, department, oldValue, cancellationToken);
            return Map(department);
        }

        private async Task<User> EnsureAuthenticatedActiveAsync(
            CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");

            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản không hoạt động.");

            return user;
        }

        private async Task<User> EnsureAdminAsync(
            CancellationToken cancellationToken)
        {
            var user = await EnsureAuthenticatedActiveAsync(cancellationToken);

            if (user.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException("Chỉ Admin được quản lý khoa/phòng ban.");

            return user;
        }

        private async Task<Department> GetOrThrowAsync(
            int departmentId,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(departmentId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy khoa/phòng ban có ID {departmentId}.");
        }

        private async Task SaveWithAuditAsync(
            int actorId,
            Department department,
            object oldValue,
            CancellationToken cancellationToken)
        {
            await _auditLogWriter.WriteAsync(
                actorId,
                AuditActionType.Update,
                nameof(Department),
                department.DepartmentId,
                oldValue,
                Snapshot(department),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static void ValidateDepartmentName(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                throw new ArgumentException(
                    "Tên khoa/phòng ban không được để trống.");
            }
        }

        private static object Snapshot(Department department)
        {
            return new
            {
                department.DepartmentId,
                department.DepartmentName,
                department.Description,
                Status = department.Status.ToString()
            };
        }

        private static DepartmentResponse Map(Department department)
        {
            return new DepartmentResponse
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                Description = department.Description,
                Status = department.Status
            };
        }
    }
}
