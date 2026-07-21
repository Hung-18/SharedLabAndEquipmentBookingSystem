using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO?> LoginAsync(
            LoginRequestDTO loginDTO,
            CancellationToken cancellationToken);

        Task<bool> LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken);
    }
}
