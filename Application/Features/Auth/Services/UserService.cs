using Application.DTOs.Auth;
using Application.Interfaces;
using AutoMapper;
using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
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
        private readonly IAuditLogWriter _auditLogWriter;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailService emailService,
            IAuditLogWriter auditLogWriter,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
            _auditLogWriter = auditLogWriter;
            _configuration = configuration;
        }

        public async Task<UserDTO?> GetUserByIdServiceAsync(
            CancellationToken cancellationToken)
        {
            int? currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue) return null;

            var user = await _userRepository.GetUserByIdAsync(
                currentUserId.Value,
                cancellationToken);
            if (user is null) return null;

            if (user.TryUnlockExpiredRestriction(DateTime.UtcNow))
            {
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return _mapper.Map<UserDTO>(user);
        }

        public async Task<AuthResponseDTO?> RefreshTokenAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.RefreshToken)) return null;

            var storedToken = await _refreshTokenRepository.GetByTokenAsync(
                request.RefreshToken,
                cancellationToken);
            if (storedToken is null) return null;

            if (!storedToken.IsActive)
            {
                if (storedToken.ExpiresAt <= DateTime.UtcNow
                    && storedToken.Status == RefreshTokenStatus.Active)
                {
                    storedToken.MarkExpired();
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return null;
            }

            var user = await _userRepository.GetUserByIdAsync(
                storedToken.UserId,
                cancellationToken);
            if (user is null) return null;
            user.TryUnlockExpiredRestriction(DateTime.UtcNow);

            if (user.Status is UserStatus.Inactive or UserStatus.Locked)
            {
                await _refreshTokenRepository.RevokeAllByUserIdAsync(
                    user.UserId,
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return null;
            }

            string accessToken = _jwtService.GenerateAccessToken(user);
            string refreshToken = _jwtService.GenerateRefreshToken();
            storedToken.Revoke();
            await _refreshTokenRepository.AddRefreshTokenAsync(
                new RefreshToken(user.UserId, refreshToken, DateTime.UtcNow.AddDays(7)));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<UserDTO> CreateUserAsync(
            CreateUserDTO request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateNewUserRequest(request);
            int actorId = _currentUserService.GetRequiredUserId();
            var actor = await _userRepository.GetUserByIdAsync(actorId, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy Admin có ID {actorId}.");
            if (actor.Status != UserStatus.Active || actor.Role?.RoleName != RoleName.Admin)
                throw new UnauthorizedAccessException("Chỉ Admin Active được tạo người dùng.");

            var role = await _unitOfWork.Roles.GetByIdAsync((int)request.Role, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy role {request.Role}.");
            if (role.RoleName != request.Role)
                throw new InvalidOperationException("RoleId và RoleName không khớp.");

            var department = await _unitOfWork.Departments.GetByIdAsync(
                request.DepartmentId,
                cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy khoa/phòng ban có ID {request.DepartmentId}.");
            if (department.Status != DepartmentStatus.Active)
                throw new InvalidOperationException(
                    "Không thể tạo user trong khoa/phòng ban đã ngừng sử dụng.");

            if (await _userRepository.IsUsernameExistsAsync(
                    request.Username,
                    cancellationToken: cancellationToken))
                throw new InvalidOperationException("Username đã tồn tại trong hệ thống.");
            if (await _userRepository.IsEmailExistsAsync(
                    request.Email,
                    cancellationToken: cancellationToken))
                throw new InvalidOperationException("Email đã tồn tại trong hệ thống.");

            var user = new User(
                (int)request.Role,
                request.DepartmentId,
                request.FullName,
                request.Username,
                request.Email,
                BCrypt.Net.BCrypt.HashPassword(request.Password));

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _userRepository.AddUserAsync(user, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _auditLogWriter.WriteAsync(
                    actor.UserId,
                    AuditActionType.Create,
                    nameof(User),
                    user.UserId,
                    null,
                    new
                    {
                        user.UserId,
                        user.FullName,
                        user.Username,
                        user.Email,
                        user.RoleId,
                        user.DepartmentId,
                        Status = user.Status.ToString()
                    },
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return _mapper.Map<UserDTO>(user);
        }

        public async Task<bool> ForgotPasswordAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            string normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null) return true;

            var oldTokens = await _passwordResetTokenRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);
            if (oldTokens.Count > 0)
                await _passwordResetTokenRepository.RemoveRangeAsync(oldTokens, cancellationToken);

            string rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            string tokenHash = HashToken(rawToken);
            await _passwordResetTokenRepository.AddAsync(
                new PasswordResetToken(
                    user.UserId,
                    normalizedEmail,
                    tokenHash,
                    DateTime.UtcNow.AddHours(1)),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            string frontendUrl = _configuration["PasswordReset:FrontendUrl"]
                ?? "https://localhost:4200";
            string resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?token="
                + $"{Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(normalizedEmail)}";
            string emailBody = $"<h2>Yêu cầu đặt lại mật khẩu</h2>"
                + $"<p>Tài khoản: <b>{normalizedEmail}</b></p>"
                + $"<p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>"
                + "<p>Liên kết hết hạn sau 1 giờ.</p>";
            await _emailService.SendEmailAsync(normalizedEmail, "Reset password", emailBody);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Token))
                return false;
            ValidatePassword(request.NewPassword);

            string normalizedEmail = request.Email.Trim().ToLowerInvariant();
            string tokenHash = HashToken(request.Token);
            var tokenRecord = await _passwordResetTokenRepository.GetByTokenHashAsync(
                normalizedEmail,
                tokenHash,
                cancellationToken);
            if (tokenRecord is null || tokenRecord.IsExpired(DateTime.UtcNow))
                return false;

            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null) return false;

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                user.SetPassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                _userRepository.Update(user);
                await _passwordResetTokenRepository.DeleteAsync(tokenRecord, ct);
                await _refreshTokenRepository.RevokeAllByUserIdAsync(user.UserId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
            return true;
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

        private static void ValidateNewUserRequest(CreateUserDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Username không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email không được để trống.");
            if (!Enum.IsDefined(request.Role))
                throw new ArgumentException("Role không hợp lệ.");
            if (request.DepartmentId <= 0)
                throw new ArgumentException("DepartmentId phải lớn hơn 0.");
            ValidatePassword(request.Password);
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự.");
            if (!password.Any(char.IsUpper)
                || !password.Any(char.IsLower)
                || !password.Any(char.IsDigit))
                throw new ArgumentException(
                    "Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        }
    }
}
