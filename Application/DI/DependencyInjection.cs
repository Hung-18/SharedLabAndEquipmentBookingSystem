using Application.Interfaces;
using Application.Services; // or AutoMapper.Extensions.Microsoft.DependencyInjection
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace Application.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection servicces)
        {
            // provide a config action so overload resolution is unambiguous
            servicces.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

            servicces.AddScoped<IJwtService,JWTService>();
            servicces.AddScoped<IUserService, UserService>();
            servicces.AddScoped<IAuthService, AuthService>();
            servicces.AddScoped<ICurrentUserService, CurrentUserService>();
           servicces.AddScoped<ILabRoomService, LabRoomService>();
            servicces.AddScoped<IEquipmentService, EquipmentService>();
            servicces.AddScoped<IMaintenanceService, MaintenanceService>();
            servicces.AddScoped<IBookingService, BookingService>();
            servicces.AddScoped<IWaitlistService, WaitlistService>();
            servicces.AddScoped<IUsageLogService, UsageLogService>();
            servicces.AddScoped<IViolationService, ViolationService>();
            servicces.AddScoped<IPriorityRuleService, PriorityRuleService>();
            servicces.AddScoped<INotificationService, NotificationService>();
            servicces.AddScoped<IAuditLogService, AuditLogService>();
            servicces.AddScoped<IAuditLogWriter, AuditLogWriter>();
            return servicces;
        }
    }
}
