using Application.DTOs.Auth;
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
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly ICurrentUserService _currentUserService;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AuthService(
     ICurrentUserService currentUserService,
     IJwtService jwtService,
     IUserRepository userRepository,
     IRefreshTokenRepository refreshTokenRepository,
     IUnitOfWork unitOfWork,
     IMapper mapper,
     IAuditLogWriter auditLogWriter)
        {
            _currentUserService = currentUserService;
            _jwtService = jwtService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogWriter = auditLogWriter;
        }

        public async Task<AuthResponseDTO?> LoginAsync(
     LoginRequestDTO request,
     CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            var user =
                await _userRepository.GetByEmailAsync(
                    request.Email,
                    cancellationToken);

            if (user is null)
            {
                return null;
            }

            bool passwordIsValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!passwordIsValid)
            {
                return null;
            }

            // Nếu restriction đã hết hạn thì tự mở khóa.
            user.TryUnlockExpiredRestriction(
                DateTime.UtcNow);

            // Hai trạng thái này không được đăng nhập.
            if (user.Status is
                UserStatus.Inactive or UserStatus.Locked)
            {
                return null;
            }

            // Chỉ Active và Restricted được đăng nhập.
            if (user.Status is not
                UserStatus.Active and not UserStatus.Restricted)
            {
                return null;
            }

            string accessToken =
                _jwtService.GenerateAccessToken(user);

            string refreshToken =
                _jwtService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken(
                user.UserId,
                refreshToken,
                DateTime.UtcNow.AddDays(7));

            await _refreshTokenRepository.AddRefreshTokenAsync(
     refreshTokenEntity);

            await _auditLogWriter.WriteAsync(
                user.UserId,
                AuditActionType.Login,
                nameof(User),
                user.UserId,
                null,
                new
                {
                    user.UserId,
                    user.Email,
                    Status = user.Status.ToString(),
                    LoginAt = DateTime.UtcNow
                },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
            var response =
    _mapper.Map<AuthResponseDTO>(user);

            response.AccessToken = accessToken;
            response.RefreshToken = refreshToken;

            return response;
        }

        public async Task<bool> LogoutAsync(
    string refreshToken,
    CancellationToken cancelationToken)
        {
            var storedToken =
                await _refreshTokenRepository.GetByTokenAsync(
                    refreshToken,
                    cancelationToken);

            if (storedToken == null
                || !storedToken.IsActive
                || storedToken.Status
                    == RefreshTokenStatus.Revoked)
            {
                return false;
            }

            storedToken.Revoke();

            await _auditLogWriter.WriteAsync(
                storedToken.UserId,
                AuditActionType.Logout,
                nameof(User),
                storedToken.UserId,
                null,
                new
                {
                    storedToken.UserId,
                    LogoutAt = DateTime.UtcNow
                },
                cancelationToken);

            await _unitOfWork.SaveChangesAsync(
                cancelationToken);

            return true;
        }
    }
}
