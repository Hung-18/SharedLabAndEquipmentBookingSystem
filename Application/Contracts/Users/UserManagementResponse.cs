using Domain;

namespace Application.DTOs.Users
{
    public class UserManagementResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int PenaltyPoints { get; set; }
        public DateTime? RestrictionUntil { get; set; }
        public UserStatus Status { get; set; }
    }
}
