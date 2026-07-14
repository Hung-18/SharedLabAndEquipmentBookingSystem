using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginDTO, CancellationToken cancelation);
        Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellation);
    }
}
