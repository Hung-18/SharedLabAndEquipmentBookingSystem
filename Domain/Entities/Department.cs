

namespace Domain.Entities
{
    public class Department
    {
        protected Department() { }

        public Department(
            string departmentName,
            string? description = null,
            DepartmentStatus status = DepartmentStatus.Active)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
                throw new ArgumentException("Tên khoa/bộ môn không được để trống");

            DepartmentName = departmentName.Trim();
            Description = description?.Trim();
            Status = status;
        }

        public int DepartmentId { get; private set; }

        public string DepartmentName { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public DepartmentStatus Status { get; private set; } = DepartmentStatus.Active;

        public ICollection<User> Users { get; private set; } = new List<User>();
    }
}