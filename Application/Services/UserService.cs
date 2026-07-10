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
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, IJwtService jwtService, IRefreshTokenRepository refreshTokenRopository, IUnitOfWork unitOfWork, IMapper iMapper)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRopository;
            _unitOfWork = unitOfWork;
            _mapper = iMapper;
        }
        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken)
        {
            var user = await _userRepository.GetByEmailAsync(loginRequestDTO.Email, cancelationToken);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDTO.Password, user.PasswordHash))
            {
                return null;
            }

            if(user.Status != UserStatus.Active)
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

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest, CancellationToken cancelationToken = default)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenRequest.RefreshToken,cancelationToken);
            if (storedToken == null)
            {
                return null;
            }
            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                var allToken = await _refreshTokenRepository.GetActiveByUserIdAsync(storedToken.UserId, cancelationToken);
                foreach (var token in allToken)
                {
                    token.Revoke();
                }
                await _unitOfWork.SaveChangesAsync();
                return null;
            }
            var user = await _userRepository.GetUserByIdAsync(storedToken.UserId, cancelationToken);
            if (user == null || user.Status != UserStatus.Active)
            {
                return null;
            }
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            storedToken.Revoke();
            var newTokenEntity = new RefreshToken(
                user.UserId,
                newRefreshToken,
                DateTime.UtcNow.AddDays(7)
            );
            await _refreshTokenRepository.AddRefreshTokenAsync(newTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancelationToken);
            return new AuthResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
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
