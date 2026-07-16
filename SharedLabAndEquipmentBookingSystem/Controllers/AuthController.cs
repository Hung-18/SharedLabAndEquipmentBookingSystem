using Application.DTOs.Auth;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public AuthController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByIdServiceAsync(cancellationToken);
            return user is null
                ? NotFound(new { message = "Không tìm thấy người dùng hiện tại." })
                : Ok(user);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token không được để trống." });
            var result = await _userService.RefreshTokenAsync(request, cancellationToken);
            return result is null
                ? Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." })
                : Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDTO request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return result is null
                ? Unauthorized(new { message = "Email hoặc mật khẩu không đúng." })
                : Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token không được để trống." });
            return await _authService.LogoutAsync(request.RefreshToken, cancellationToken)
                ? Ok(new { message = "Đăng xuất thành công." })
                : BadRequest(new { message = "Refresh token không hợp lệ." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserDTO request,
            CancellationToken cancellationToken)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                await _userService.CreateUserAsync(request, cancellationToken));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken cancellationToken)
        {
            await _userService.ForgotPasswordAsync(request.Email, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "Nếu email tồn tại, hệ thống đã gửi liên kết đặt lại mật khẩu."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            bool success = await _userService.ResetPasswordAsync(request, cancellationToken);
            return success
                ? Ok(new { message = "Đặt lại mật khẩu thành công." })
                : BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });
        }
    }
}
