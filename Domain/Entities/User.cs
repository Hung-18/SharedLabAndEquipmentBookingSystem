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
            ValidateRole(roleId);
            ValidateDepartment(departmentId);
            ValidateProfile(fullName, username, email);

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
        public UserStatus Status { get; private set; } = UserStatus.Active;
        public Role? Role { get; private set; }
        public Department? Department { get; private set; }
        public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
        public ICollection<Booking> ApprovedBookings { get; private set; } = new List<Booking>();
        public ICollection<LabRoom> ManagedLabRooms { get; private set; } = new List<LabRoom>();
        public ICollection<Maintenance> CreatedMaintenances { get; private set; } = new List<Maintenance>();
        public ICollection<Violation> Violations { get; private set; } = new List<Violation>();
        public ICollection<Waitlist> Waitlists { get; private set; } = new List<Waitlist>();
        public ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();
        public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
        public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

        public void UpdateProfile(
            string fullName,
            string username,
            string email)
        {
            ValidateProfile(fullName, username, email);
            FullName = fullName.Trim();
            Username = username.Trim();
            Email = email.Trim();
        }

        public void ChangeRole(int roleId)
        {
            ValidateRole(roleId);
            RoleId = roleId;
        }

        public void ChangeDepartment(int departmentId)
        {
            ValidateDepartment(departmentId);
            DepartmentId = departmentId;
        }

        public void AddPenaltyPoints(int points)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Điểm phạt phải lớn hơn 0.");
            }

            PenaltyPoints += points;
        }

        public void RemovePenaltyPoints(int points)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Điểm phạt cần trừ phải lớn hơn 0.");
            }

            PenaltyPoints = Math.Max(0, PenaltyPoints - points);
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

        public void Lock()
        {
            RestrictionUntil = null;
            Status = UserStatus.Locked;
        }

        public void Deactivate()
        {
            RestrictionUntil = null;
            Status = UserStatus.Inactive;
        }

        public void Activate()
        {
            RestrictionUntil = null;
            Status = UserStatus.Active;
        }

        public void Unlock()
        {
            Activate();
        }

        public void SetStatus(
            UserStatus status,
            DateTime? restrictionUntil = null)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentException("Trạng thái người dùng không hợp lệ.");
            }

            switch (status)
            {
                case UserStatus.Active:
                    Activate();
                    break;
                case UserStatus.Inactive:
                    Deactivate();
                    break;
                case UserStatus.Locked:
                    Lock();
                    break;
                case UserStatus.Restricted:
                    if (!restrictionUntil.HasValue)
                    {
                        throw new ArgumentException(
                            "Phải cung cấp RestrictionUntil khi chuyển sang Restricted.");
                    }

                    RestrictUntil(restrictionUntil.Value);
                    break;
                default:
                    throw new ArgumentException("Trạng thái người dùng không hợp lệ.");
            }
        }

        public void SetPassword(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Mật khẩu không được để trống.");
            }

            PasswordHash = passwordHash;
        }

        public bool TryUnlockExpiredRestriction(DateTime utcNow)
        {
            if (Status != UserStatus.Restricted
                || !RestrictionUntil.HasValue
                || RestrictionUntil.Value > utcNow)
            {
                return false;
            }

            Unlock();
            return true;
        }

        public bool HasActiveRestriction(DateTime utcNow)
        {
            if (Status != UserStatus.Restricted)
            {
                return false;
            }

            return !RestrictionUntil.HasValue
                || RestrictionUntil.Value > utcNow;
        }

        private static void ValidateProfile(
            string fullName,
            string username,
            string email)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Họ tên không được để trống.");

            if (fullName.Trim().Length > 150)
                throw new ArgumentException("Họ tên không được vượt quá 150 ký tự.");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username không được để trống.");

            if (username.Trim().Length > 100)
                throw new ArgumentException("Username không được vượt quá 100 ký tự.");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");

            if (email.Trim().Length > 150 || !email.Contains('@'))
                throw new ArgumentException("Email không hợp lệ.");
        }

        private static void ValidateRole(int roleId)
        {
            if (roleId <= 0)
                throw new ArgumentException("RoleId phải lớn hơn 0.");
        }

        private static void ValidateDepartment(int departmentId)
        {
            if (departmentId <= 0)
                throw new ArgumentException("DepartmentId phải lớn hơn 0.");
        }
    }
}
