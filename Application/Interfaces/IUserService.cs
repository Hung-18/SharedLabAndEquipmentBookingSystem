using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDTO?> RefreshTokenAsync(
            RefreshTokenRequest refreshToken,
            CancellationToken cancellationToken = default);

        Task<UserDTO?> GetUserByIdServiceAsync(
            CancellationToken cancellationToken = default);

        Task<UserDTO> CreateUserAsync(
            CreateUserDTO createUserDTO,
            CancellationToken cancellationToken = default);

        Task<bool> ForgotPasswordAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(
            ResetPasswordRequest resetPasswordRequest,
            CancellationToken cancellationToken = default);
    }
}
