using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Departments
{
    public class DepartmentResponse
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DepartmentStatus Status { get; set; }
    }

}
