using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
