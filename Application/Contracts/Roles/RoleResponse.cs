using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Roles
{
    public class RoleResponse
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

}
