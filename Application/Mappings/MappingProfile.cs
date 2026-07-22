using Application.DTOs.AuditLogs;
using Application.DTOs.Auth;
using Application.DTOs.Booking;
using Application.DTOs.Departments;
using Application.DTOs.Equipments;
using Application.DTOs.LabRooms;
using Application.DTOs.Maintenances;
using Application.DTOs.Notifications;
using Application.DTOs.PriorityRules;
using Application.DTOs.Roles;
using Application.DTOs.UsageLogs;
using Application.DTOs.Users;
using Application.DTOs.Violations;
using Application.DTOs.Waitlists;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// Maps Domain entities to Application response contracts.
    ///
    /// Request DTOs are intentionally not mapped directly to entities because
    /// entities use constructors and domain methods to enforce business rules.
    /// </summary>
    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ConfigureAuthMappings();
            ConfigureAuditLogMappings();
            ConfigureBookingMappings();
            ConfigureDepartmentMappings();
            ConfigureEquipmentMappings();
            ConfigureLabRoomMappings();
            ConfigureMaintenanceMappings();
            ConfigureNotificationMappings();
            ConfigurePriorityRuleMappings();
            ConfigureRoleMappings();
            ConfigureUsageLogMappings();
            ConfigureUserMappings();
            ConfigureViolationMappings();
            ConfigureWaitlistMappings();
        }

        private void ConfigureAuthMappings()
        {
            CreateMap<User, AuthResponseDTO>()
                .ForMember(
                    destination => destination.AccessToken,
                    option => option.Ignore())
                .ForMember(
                    destination => destination.RefreshToken,
                    option => option.Ignore());
        }

        private void ConfigureAuditLogMappings()
        {
            CreateMap<AuditLog, AuditLogResponse>()
                .ForMember(
                    destination => destination.UserName,
                    option => option.MapFrom(
                        source => source.User != null
                            ? source.User.FullName
                            : null))
                .ForMember(
                    destination => destination.ActionType,
                    option => option.MapFrom(
                        source => source.ActionType.ToString()));
        }

        private void ConfigureBookingMappings()
        {
            CreateMap<BookingItem, BookingItemResponse>()
                .ForMember(
                    destination => destination.ResourceType,
                    option => option.MapFrom(
                        source => source.ResourceType.ToString()))
                .ForMember(
                    destination => destination.LabName,
                    option => option.MapFrom(
                        source => source.LabRoom != null
                            ? source.LabRoom.LabName
                            : null))
                .ForMember(
                    destination => destination.EquipmentName,
                    option => option.MapFrom(
                        source => source.Equipment != null
                            ? source.Equipment.EquipmentName
                            : null));

            CreateMap<Booking, BookingResponse>()
                .ForMember(
                    destination => destination.PriorityLevel,
                    option => option.MapFrom(
                        source => source.PriorityRule != null
                            ? source.PriorityRule.PriorityLevel
                            : (int?)null))
                .ForMember(
                    destination => destination.PurposeType,
                    option => option.MapFrom(
                        source => source.PurposeType.ToString()))
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));

            CreateMap<Booking, BookingDetailResponse>()
                .ForMember(
                    destination => destination.UserName,
                    option => option.MapFrom(
                        source => source.User != null
                            ? source.User.FullName
                            : null))
                .ForMember(
                    destination => destination.PriorityLevel,
                    option => option.MapFrom(
                        source => source.PriorityRule != null
                            ? source.PriorityRule.PriorityLevel
                            : (int?)null))
                .ForMember(
                    destination => destination.ApprovedByName,
                    option => option.MapFrom(
                        source => source.ApprovedBy != null
                            ? source.ApprovedBy.FullName
                            : null))
                .ForMember(
                    destination => destination.PurposeType,
                    option => option.MapFrom(
                        source => source.PurposeType.ToString()))
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()))
                .ForMember(
                    destination => destination.Items,
                    option => option.MapFrom(
                        source => source.BookingItems));
        }

        private void ConfigureDepartmentMappings()
        {
            CreateMap<Department, DepartmentResponse>();
        }

        private void ConfigureEquipmentMappings()
        {
            CreateMap<Equipment, EquipmentResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));

            CreateMap<Equipment, EquipmentDetailResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));
        }

        private void ConfigureLabRoomMappings()
        {
            CreateMap<LabRoom, LabRoomResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));

            CreateMap<LabRoom, LabRoomDetailResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()))
                .ForMember(
                    destination => destination.ManagerName,
                    option => option.MapFrom(
                        source => source.Manager != null
                            ? source.Manager.FullName
                            : null));
        }

        private void ConfigureMaintenanceMappings()
        {
            CreateMap<Maintenance, MaintenanceResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()))
                .ForMember(
                    destination => destination.RecurrenceType,
                    option => option.MapFrom(
                        source => source.RecurrenceType.ToString()));

            CreateMap<Maintenance, MaintenanceDetailResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()))
                .ForMember(
                    destination => destination.RecurrenceType,
                    option => option.MapFrom(
                        source => source.RecurrenceType.ToString()));
        }

        private void ConfigureNotificationMappings()
        {
            CreateMap<Notification, NotificationResponse>()
                .ForMember(
                    destination => destination.NotificationType,
                    option => option.MapFrom(
                        source => source.NotificationType.ToString()));
        }

        private void ConfigurePriorityRuleMappings()
        {
            CreateMap<PriorityRule, PriorityRuleResponse>()
                .ForMember(
                    destination => destination.PurposeType,
                    option => option.MapFrom(
                        source => source.PurposeType.ToString()))
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));
        }

        private void ConfigureRoleMappings()
        {
            CreateMap<Role, RoleResponse>()
                .ForMember(
                    destination => destination.RoleName,
                    option => option.MapFrom(
                        source => source.RoleName.ToString()));
        }

        private void ConfigureUsageLogMappings()
        {
            CreateMap<UsageLog, UsageLogResponse>()
                .ForMember(
                    destination => destination.IncidentStatus,
                    option => option.MapFrom(
                        source => source.IncidentStatus.ToString()))
                .ForMember(
                    destination => destination.IncidentReviewStatus,
                    option => option.MapFrom(
                        source => source.IncidentReviewStatus.ToString()));
        }

        private void ConfigureUserMappings()
        {
            CreateMap<User, UserDTO>()
                .ForMember(
                    destination => destination.RoleName,
                    option => option.MapFrom(
                        source => source.Role != null
                            ? source.Role.RoleName.ToString()
                            : string.Empty))
                .ForMember(
                    destination => destination.DepartmentName,
                    option => option.MapFrom(
                        source => source.Department != null
                            ? source.Department.DepartmentName
                            : string.Empty));

            CreateMap<User, UserManagementResponse>()
                .ForMember(
                    destination => destination.RoleName,
                    option => option.MapFrom(
                        source => source.Role != null
                            ? source.Role.RoleName.ToString()
                            : string.Empty))
                .ForMember(
                    destination => destination.DepartmentName,
                    option => option.MapFrom(
                        source => source.Department != null
                            ? source.Department.DepartmentName
                            : string.Empty));
        }

        private void ConfigureViolationMappings()
        {
            CreateMap<global::Violation, ViolationResponse>()
                .ForMember(
                    destination => destination.ViolationType,
                    option => option.MapFrom(
                        source => source.ViolationType.ToString()))
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));
        }

        private void ConfigureWaitlistMappings()
        {
            CreateMap<Waitlist, WaitlistResponse>()
                .ForMember(
                    destination => destination.Status,
                    option => option.MapFrom(
                        source => source.Status.ToString()));
        }
    }
}
