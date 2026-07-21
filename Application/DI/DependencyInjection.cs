using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddAutoMapper(
                configuration => { },
                typeof(DependencyInjection).Assembly);

            services.AddScoped<IJwtService, JWTService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ILabRoomService, LabRoomService>();
            services.AddScoped<IEquipmentService, EquipmentService>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IWaitlistService, WaitlistService>();
            services.AddScoped<IUsageLogService, UsageLogService>();
            services.AddScoped<IViolationService, ViolationService>();
            services.AddScoped<IPriorityRuleService, PriorityRuleService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuditLogWriter, AuditLogWriter>();

            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

            return services;
        }
    }
}
