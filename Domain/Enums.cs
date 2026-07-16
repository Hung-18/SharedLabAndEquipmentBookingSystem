using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public enum RoleName
    {
        Admin = 1,
        LabManager = 2,
        Requester = 3
    }

    public enum DepartmentStatus
    {
        Active = 1,
        Inactive = 2
    }

    public enum UserStatus
    {
        Active = 1,
        Inactive = 2,
        Restricted = 3,
        Locked = 4
    }

    public enum BookingPurposeType
    {
        ResearchProject = 1,
        CoursePractice = 2,
        SelfStudy = 3,
        Other = 4
    }

    public enum BookingStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        Completed = 5,
        NoShow = 6
    }

    public enum ResourceType
    {
        LabRoom = 1,
        Equipment = 2
    }

    public enum LabRoomStatus
    {
        Available = 1,
        Unavailable = 2,
        Maintenance = 3,
        Inactive = 4
    }

    public enum EquipmentStatus
    {
        Available = 1,
        InUse = 2,
        Maintenance = 3,
        Broken = 4,
        Retired = 5
    }

    public enum MaintenanceStatus
    {
        Scheduled = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum MaintenanceRecurrenceType
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }

    public enum WaitlistStatus
    {
        Waiting = 1,
        Notified = 2,
        Booked = 3,
        Cancelled = 4,
        Expired = 5
    }

    public enum ViolationType
    {
        NoShow = 1,
        LateCheckout = 2,
        DamageEquipment = 3,
        MisuseEquipment = 4,
        UnauthorizedUse = 5
    }

    public enum ViolationStatus
    {
        Active = 1,
        Resolved = 2,
        Cancelled = 3
    }

    public enum UsageIncidentStatus
    {
        None = 1,
        DamageReported = 2,
        LateCheckout = 3,
        MissingEquipment = 4,
        Other = 5
    }

    public enum PriorityRuleStatus
    {
        Active = 1,
        Inactive = 2
    }

    public enum AuditActionType
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        Login = 4,
        Logout = 5,
        ApproveBooking = 6,
        RejectBooking = 7,
        CheckIn = 8,
        CheckOut = 9
    }

    public enum RefreshTokenStatus
    {
        Active = 1,
        Revoked = 2,
        Expired = 3
    }

    public enum NotificationType
    {
        BookingApproved = 1,
        BookingRejected = 2,
        BookingReminder = 3,
        WaitlistAvailable = 4,
        Maintenance = 5,
        Violation = 6,
        System = 7
    }
}

