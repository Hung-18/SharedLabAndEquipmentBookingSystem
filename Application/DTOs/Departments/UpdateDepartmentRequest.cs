using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Departments
{
    public class UpdateDepartmentRequest
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

}
