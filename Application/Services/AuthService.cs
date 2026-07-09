using Application.DTOs;
using Application.Interfaces;
using BCrypt.Net;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        public AuthService(ICurrentUserService currentUserService, IJwtService jwtService, IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository)
        {
            _currentUserService = currentUserService;
            _jwtService = jwtService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginDTO)
        {
            var user = await _userRepository.GetByEmailAsync(loginDTO.Email);
            if(user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var hashedToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);

            var refreshTokenEntity = new RefreshToken
                (
                    user.UserId,
                    hashedToken,
                    DateTime.UtcNow.AddDays(7)      
                );
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            return new AuthResponseDTO
            {   
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
