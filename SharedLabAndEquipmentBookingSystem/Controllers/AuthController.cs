using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthService _authService;


        public AuthController(IUserService userService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService)
        {
            _userService = userService;
            _refreshTokenRepository = refreshTokenRepository;
            _authService = authService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancelationToken)
        {
            //var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Can't find user in token");
            //if(!int.TryParse(userIdString, out var userId))
            //{
            //    return Unauthorized("Invalid token");
            //}
            var user = _userService.GetUserByIdServiceAsync(cancelationToken);
            if (user == null) return NotFound();
            return Ok();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest refreshToken)
        {
            var newToken = await _userService.RefreshTokenAsync(refreshToken);
            if (newToken == null) return Unauthorized("Invalid Token");
            return Ok(newToken);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken)
        {
            var authResponse = await _authService.LoginAsync(loginRequestDTO, cancelationToken);
            if (authResponse == null) return Unauthorized("Invalid Email or Password");
            return Ok(authResponse);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest refreshToken, CancellationToken cancelationToken)
        {
            if (string.IsNullOrEmpty(refreshToken.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }
            var result = await _authService.LogoutAsync(refreshToken.RefreshToken, cancelationToken);
            if (!result) return BadRequest("Invalid Token");
            return Ok("Logged out successfully");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO createUserDTO, CancellationToken cancelationToken)
        {
            var user = await _userService.CreateUserAsync(createUserDTO, cancelationToken);
            return StatusCode(StatusCodes.Status201Created, user);
        }

        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody]ForgotPasswordRequest request, CancellationToken cancelationToken)
        {
            var user = await _userService.ForgotPasswordAsync(request.Email, cancelationToken);
            return Ok(new
            {
                Success = true,
                Message = "Nếu email này tồn tại trong hệ thống, chúng tôi đã gửi liên kết đặt lại mật khẩu cho bạn."
            });
        }
    }
}
