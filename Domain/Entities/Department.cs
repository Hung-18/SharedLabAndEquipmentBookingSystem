using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Department
    {
        protected Department()
        {
        }

        public Department(
            string departmentName,
            string? description = null,
            DepartmentStatus status = DepartmentStatus.Active)
        {
            Validate(departmentName, description);

            if (!Enum.IsDefined(status))
                throw new ArgumentException("Trạng thái khoa/phòng ban không hợp lệ.");

            DepartmentName = departmentName.Trim();
            Description = description?.Trim();
            Status = status;
        }

        public int DepartmentId { get; private set; }
        public string DepartmentName { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public DepartmentStatus Status { get; private set; } = DepartmentStatus.Active;
        public ICollection<User> Users { get; private set; } = new List<User>();

        public void UpdateDetails(
            string departmentName,
            string? description)
        {
            Validate(departmentName, description);
            DepartmentName = departmentName.Trim();
            Description = description?.Trim();
        }

        public void Activate()
        {
            Status = DepartmentStatus.Active;
        }

        public void Deactivate()
        {
            Status = DepartmentStatus.Inactive;
        }

        private static void Validate(
            string departmentName,
            string? description)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
                throw new ArgumentException("Tên khoa/phòng ban không được để trống.");

            if (departmentName.Trim().Length > 150)
                throw new ArgumentException("Tên khoa/phòng ban không được vượt quá 150 ký tự.");

            if (description?.Trim().Length > 500)
                throw new ArgumentException("Mô tả không được vượt quá 500 ký tự.");
        }
    }
}
