using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class JWTService : IJwtService
    {
        private readonly IConfiguration _configuration;
        public JWTService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(User user)
        {
            var jwtSetting = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            string roleName = user.Role?.RoleName.ToString() ?? "Requester";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
            };

            var token = new JwtSecurityToken
                (
                    issuer: jwtSetting["Issuer"],
                    audience: jwtSetting["Audience"],
                    claims: claims,
                    expires:DateTime.UtcNow.AddMinutes(45),
                    signingCredentials: creds
                );
            return new JwtSecurityTokenHandler().WriteToken(token); 
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            var rgn = RandomNumberGenerator.Create();
            rgn.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
