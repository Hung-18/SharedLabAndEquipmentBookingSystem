using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
    }
}
