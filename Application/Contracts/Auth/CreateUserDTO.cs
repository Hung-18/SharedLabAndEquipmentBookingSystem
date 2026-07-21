using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class CreateUserDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int DepartmentId { get; set; } 
        public RoleName Role { get; set; }
    }
}
