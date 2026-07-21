using System;
using System.Collections.Generic;
using System.Text;

using Domain;

namespace Application.DTOs.Auth
{
    public class UserDTO
    {
        public int UserId { get; set; }

        public string FullName { get; set; }
            = string.Empty;

        public string Username { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string RoleName { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        public int PenaltyPoints { get; set; }

        public DateTime? RestrictionUntil { get; set; }

        public UserStatus Status { get; set; }
    }
}
