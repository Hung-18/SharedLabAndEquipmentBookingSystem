using Application.DTOs.Auth;
using Application.Interfaces;
using AutoMapper;
using BCrypt.Net;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
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
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailService _emailService;
        public UserService(IUserRepository userRepository, IJwtService jwtService, IRefreshTokenRepository refreshTokenRopository, IUnitOfWork unitOfWork, IMapper iMapper, ICurrentUserService currentUserService, IPasswordResetTokenRepository passwordResetTokenRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRopository;
            _unitOfWork = unitOfWork;
            _mapper = iMapper;
            _currentUserService = currentUserService;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
        }

        public async Task<UserDTO> GetUserByIdServiceAsync(CancellationToken cancelation)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null) return null;

            var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancelation);
            if (user == null) return null;
            var newUser = _mapper.Map<UserDTO>(user);
            return newUser;
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest, CancellationToken cancelationToken = default)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenRequest.RefreshToken, cancelationToken);
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

        

        public async Task<UserDTO> CreateUserAsync(CreateUserDTO createUserDTO, CancellationToken cancelation)
        {
            var existUser = await _userRepository.IsUsernameExistsAsync(createUserDTO.Email);
            if (existUser) throw new InvalidOperationException("UserNawe exist in system");
            var existEmail = await _userRepository.IsEmailExistsAsync(createUserDTO.Username);
            if (existEmail) throw new InvalidOperationException("Email exist in system");
            var hash = BCrypt.Net.BCrypt.HashPassword(createUserDTO.Password);

            var roleId = (int)createUserDTO.Role;
            var newUser = new User
                (
                    roleId: roleId,
                    departmentId: createUserDTO.DepartmentId,
                    fullName: createUserDTO.FullName,
                    username: createUserDTO.Username,
                    email: createUserDTO.Email,
                    passwordHash: hash
                );

            await _userRepository.AddUserAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserDTO>(newUser);
        }

        public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancelation = default)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return false;
            }
            var oldToken = await _passwordResetTokenRepository.GetByEmailAsync(email);
            if (oldToken.Any())
            {
                await _passwordResetTokenRepository.RemoveRangeAsync(oldToken);
            }
            // Generate a password reset token
            var token = Guid.NewGuid().ToString();
            var expiryDate = DateTime.UtcNow.AddHours(1);
            var passwordResetToken = new PasswordResetToken
            {
                Email = email,
                Token = token,
                ExpiryDate = expiryDate
            };
            await _passwordResetTokenRepository.AddAsync(passwordResetToken);
            await _unitOfWork.SaveChangesAsync();

            string frontendUrl = "https://localhost:4200";
            string encodedToken = Uri.EscapeDataString(token);
            string encodedEmail = Uri.EscapeDataString(email);
            string resetLink = $"{frontendUrl}/reset-password?token={encodedToken}&email={encodedEmail}";
            string emailBody = $@"
        <h2>Yêu cầu đặt lại mật khẩu</h2>
        <p>Chào sếp,</p>
        <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản: <b>{email}</b></p>
        <p>Vui lòng nhấp vào liên kết dưới đây để thiết lập mật khẩu mới:</p>
        <a href='{resetLink}'>Đặt lại mật khẩu</a>
        <p>Liên kết này sẽ hết hạn sau 1 giờ.</p>";
            await _emailService.SendEmailAsync(email, "Reset password", emailBody);
            Console.WriteLine($"Token of {email} is {token}");
            return true;
        }
        public async Task<bool> ResetPasswordAsync(DTOs.Auth.ResetPasswordRequest request, CancellationToken cancellation)
        {
            // 1. Tìm token
            var tokenRecord = await _passwordResetTokenRepository.GetByTokenAsync(request.Email, request.Token);
            if (tokenRecord == null || tokenRecord.ExpiryDate < DateTime.UtcNow) return false;

            // 2. Tìm user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) return false;

            var expiry = DateTime.UtcNow.AddHours(1);
            if (expiry <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid time");
            }

            // 3. Update mật khẩu (Gán trực tiếp hoặc dùng phương thức Domain)
            user.SetPassword(BCrypt.Net.BCrypt.HashPassword(request.newPassword));

            // 4. Xóa token
            await _passwordResetTokenRepository.DeleteAsync(tokenRecord);

            // 5. Lưu
            await _unitOfWork.SaveChangesAsync();



            return true;
        }
    }
}
