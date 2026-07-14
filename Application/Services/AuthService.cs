using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using BCrypt.Net;
using Domain;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AuthService(ICurrentUserService currentUserService, IJwtService jwtService, IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfwork, IMapper mapper)
        {
            _currentUserService = currentUserService;
            _jwtService = jwtService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfwork;
            _mapper = mapper;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken)
        {
            var user = await _userRepository.GetByEmailAsync(loginRequestDTO.Email, cancelationToken);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDTO.Password, user.PasswordHash))
            {
                return null;
            }

            if (user.Status != UserStatus.Active)
            {
                return null;
            }
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            //var hashToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);

            var refreshTokenEntity = new RefreshToken(
                user.UserId,
                refreshToken,
                DateTime.UtcNow.AddDays(7)
            );
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<AuthResponseDTO>(user);
            response.AccessToken = accessToken;
            response.RefreshToken = refreshToken;

            return response;
        }

        public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancelationToken)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancelationToken);
            if (storedToken == null || !storedToken.IsActive || storedToken.Status == RefreshTokenStatus.Revoked)
            {
                return false;
            }

            storedToken.Revoke();
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
