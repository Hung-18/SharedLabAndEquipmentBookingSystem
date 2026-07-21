using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public sealed class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

}
