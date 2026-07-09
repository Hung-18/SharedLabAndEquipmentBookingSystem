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
        public CurrentUserService(IHttpContextAccessor contextAccesor)
        {
            _contextAccessor = contextAccesor;
        }

        public int? UserId
        {
            get
            {
                var id = _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? _contextAccessor.HttpContext?.User?.FindFirstValue("UserId");
                if (string.IsNullOrEmpty(id)) return null;
                return int.TryParse(id, out var userId) ? userId : null;
            }
        }
    }
}
