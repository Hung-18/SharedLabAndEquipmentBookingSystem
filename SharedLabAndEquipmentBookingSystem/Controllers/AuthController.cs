using Application.DTOs.Auth;
using Application.Features.Auth.Commands.CreateUser;
using Application.Features.Auth.Commands.ForgotPassword;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Logout;
using Application.Features.Auth.Commands.RefreshToken;
using Application.Features.Auth.Commands.ResetPassword;
using Application.Features.Auth.Queries.GetUserByIdService;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            var user = await _sender.Send(new UserGetUserByIdServiceQuery(), cancellationToken);
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
            var result = await _sender.Send(new UserRefreshTokenCommand(request), cancellationToken);
            return result is null
                ? Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." })
                : Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDTO request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new AuthLoginCommand(request), cancellationToken);
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
            return await _sender.Send(new AuthLogoutCommand(request.RefreshToken), cancellationToken)
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
                await _sender.Send(new UserCreateUserCommand(request), cancellationToken));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new UserForgotPasswordCommand(request.Email), cancellationToken);
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
            bool success = await _sender.Send(new UserResetPasswordCommand(request), cancellationToken);
            return success
                ? Ok(new { message = "Đặt lại mật khẩu thành công." })
                : BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });
        }
    }
}
