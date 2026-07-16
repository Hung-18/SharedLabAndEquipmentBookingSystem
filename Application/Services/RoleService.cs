using Application.DTOs.Roles;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;

        public RoleService(
            IRoleRepository repository,
            IUserRepository userRepository,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _userRepository = userRepository;
            _currentUserService = currentUserService;
        }

        public async Task<List<RoleResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            await EnsureAdminAsync(cancellationToken);
            var roles = await _repository.GetAllOrderedAsync(cancellationToken);
            return roles.Select(Map).ToList();
        }

        public async Task<RoleResponse?> GetByIdAsync(
            int roleId,
            CancellationToken cancellationToken = default)
        {
            await EnsureAdminAsync(cancellationToken);
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            return role is null ? null : Map(role);
        }

        private async Task EnsureAdminAsync(CancellationToken cancellationToken)
        {
            int userId = _currentUserService.GetRequiredUserId();
            var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy người dùng có ID {userId}.");

            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("Tài khoản không hoạt động.");

            if (user.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException("Chỉ Admin được xem danh sách role.");
        }

        private static RoleResponse Map(Role role)
        {
            return new RoleResponse
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName.ToString(),
                Description = role.Description
            };
        }
    }
}
