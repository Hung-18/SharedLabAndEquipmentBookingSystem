using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Application.DTOs.Auth
{
    public class LoginRequestDTO
    {
        public string Email { get; set; }
            = string.Empty;

        public string Password { get; set; }
            = string.Empty;
    }
}
