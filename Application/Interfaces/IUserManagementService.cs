using Application.DTOs.Users;
using Domain;

namespace Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<PagedUserResponse> SearchAsync(
            string? keyword,
            RoleName? roleName,
            int? departmentId,
            UserStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse?> GetByIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> UpdateAsync(
            int userId,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> ChangeRoleAsync(
            int userId,
            ChangeUserRoleRequest request,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> ChangeDepartmentAsync(
            int userId,
            ChangeUserDepartmentRequest request,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> LockAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> UnlockAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> DeactivateAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> ActivateAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<UserManagementResponse> SetStatusAsync(
            int userId,
            SetUserStatusRequest request,
            CancellationToken cancellationToken = default);

        Task<UserPenaltyResponse> GetPenaltyAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }
}
