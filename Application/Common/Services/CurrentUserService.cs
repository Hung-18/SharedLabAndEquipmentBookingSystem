using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CurrentUserService(
            IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public int? UserId
        {
            get
            {
                var principal =
                    _contextAccessor.HttpContext?.User;

                if (principal?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                var userIdText =
                    principal.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userIdText))
                {
                    return null;
                }

                return int.TryParse(
                    userIdText,
                    out var userId)
                        ? userId
                        : null;
            }
        }

        public int GetRequiredUserId()
        {
            return UserId
                ?? throw new UnauthorizedAccessException(
                    "Không xác định được người dùng từ access token.");
        }
    }
}
