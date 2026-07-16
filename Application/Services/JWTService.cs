using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class JWTService : IJwtService
    {
        private readonly IConfiguration _configuration;
        public JWTService(IConfiguration configuration) => _configuration = configuration;

        public string GenerateAccessToken(User user)
        {
            string keyValue = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key.");
            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Issuer.");
            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Audience.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            string roleName = user.Role?.RoleName.ToString() ?? "Requester";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(45),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
