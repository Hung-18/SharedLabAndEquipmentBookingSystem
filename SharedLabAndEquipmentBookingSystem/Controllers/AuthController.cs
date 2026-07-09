using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthController(IUserService userService, IRefreshTokenRepository refreshTokenRepository)
        {
            _userService = userService;
            _refreshTokenRepository = refreshTokenRepository;
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest refreshToken)
        {
            var newToken = await _userService.RefreshTokenAsync(refreshToken);
            if (newToken == null) return Unauthorized("Invalid Token");
            return Ok(newToken);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginRequestDTO loginRequestDTO, CancellationToken cancelationToken)
        {
            var authResponse = await _userService.LoginAsync(loginRequestDTO,cancelationToken);
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
            var result = await _userService.LogoutAsync(refreshToken.RefreshToken, cancelationToken);
            if (!result) return BadRequest("Invalid Token");
            return Ok("Logged out successfully");
        }
    }
}
