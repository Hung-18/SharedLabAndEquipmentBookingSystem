using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Repository.Email;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILabRoomRepository, LabRoomRepository>();
            services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            services.AddScoped<IPriorityRuleRepository, PriorityRuleRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IBookingItemRepository, BookingItemRepository>();
            services.AddScoped<IUsageLogRepository, UsageLogRepository>();
            services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
            services.AddScoped<IWaitlistRepository, WaitlistRepository>();
            services.AddScoped<IViolationRepository, ViolationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
