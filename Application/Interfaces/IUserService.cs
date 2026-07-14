using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserService
    {
        //Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequest refreshToken, CancellationToken cancelationToken = default);
        //Task<bool> LogoutAsync(string refreshToken, CancellationToken cancelationToken = default);
        Task<UserDTO> GetUserByIdServiceAsync (CancellationToken cancelationToken = default);
        Task<UserDTO> CreateUserAsync(CreateUserDTO createUserDTO, CancellationToken cancelationToken = default);
        Task<bool> ForgotPasswordAsync(string email, CancellationToken cancelationToken = default);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest, CancellationToken cancelationToken = default);
    }
}
