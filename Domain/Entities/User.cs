using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class User
    {
        protected User()
        {
        }

        public User(
            int roleId,
            int departmentId,
            string fullName,
            string username,
            string email,
            string passwordHash)
        {
            if (roleId <= 0)
            {
                throw new ArgumentException(
                    "RoleId phải lớn hơn 0.");
            }

            if (departmentId <= 0)
            {
                throw new ArgumentException(
                    "DepartmentId phải lớn hơn 0.");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException(
                    "Username không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException(
                    "PasswordHash không được để trống.");
            }

            RoleId = roleId;
            DepartmentId = departmentId;
            FullName = fullName.Trim();
            Username = username.Trim();
            Email = email.Trim();
            PasswordHash = passwordHash;
            PenaltyPoints = 0;
            Status = UserStatus.Active;
        }

        public int UserId { get; private set; }

        public int RoleId { get; private set; }

        public int DepartmentId { get; private set; }

        public string FullName { get; private set; } = string.Empty;

        public string Username { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;

        public int PenaltyPoints { get; private set; }

        public DateTime? RestrictionUntil { get; private set; }

        public UserStatus Status { get; private set; }
            = UserStatus.Active;

        public Role? Role { get; private set; }

        public Department? Department { get; private set; }

        public ICollection<Booking> Bookings { get; private set; }
            = new List<Booking>();

        public ICollection<Booking> ApprovedBookings { get; private set; }
            = new List<Booking>();

        public ICollection<LabRoom> ManagedLabRooms { get; private set; }
            = new List<LabRoom>();

        public ICollection<Maintenance> CreatedMaintenances { get; private set; }
            = new List<Maintenance>();

        public ICollection<Violation> Violations { get; private set; }
            = new List<Violation>();

        public ICollection<Waitlist> Waitlists { get; private set; }
            = new List<Waitlist>();

        public ICollection<AuditLog> AuditLogs { get; private set; }
            = new List<AuditLog>();

        public ICollection<RefreshToken> RefreshTokens { get; private set; }
            = new List<RefreshToken>();

        public ICollection<Notification> Notifications { get; private set; }
            = new List<Notification>();

        public void AddPenaltyPoints(int points)
        {
            if (points <= 0)
            {
                throw new ArgumentException(
                    "Điểm phạt phải lớn hơn 0.");
            }

            PenaltyPoints += points;
        }

        public void RemovePenaltyPoints(int points)
        {
            if (points <= 0)
            {
                throw new ArgumentException(
                    "Điểm phạt cần trừ phải lớn hơn 0.");
            }

            PenaltyPoints = Math.Max(
                0,
                PenaltyPoints - points);
        }

        public void RestrictUntil(DateTime restrictionUntil)
        {
            if (restrictionUntil <= DateTime.UtcNow)
            {
                throw new ArgumentException(
                    "Thời gian hạn chế phải lớn hơn thời điểm hiện tại.");
            }

            RestrictionUntil = restrictionUntil;
            Status = UserStatus.Restricted;
        }

        public void Unlock()
        {
            RestrictionUntil = null;
            Status = UserStatus.Active;
        }

        public void SetPassword(string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new ArgumentException("password hash can't empty");
            }

            PasswordHash = passwordHash;
        }

        public void Unrestric()
        {
            if(Status == UserStatus.Restricted)
            {
                Status = UserStatus.Active;
                RestrictionUntil = null;
            }
        }
    }
}