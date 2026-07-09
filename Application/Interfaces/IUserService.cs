using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequest refreshToken, CancellationToken cancelationToken = default);
        Task<bool> LogoutAsync(string refreshToken, CancellationToken cancelationToken = default);
    }
}
