using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IUnitOfWork
    {
        IRoleRepository Roles { get; }
        IDepartmentRepository Departments { get; }
        IUserRepository Users { get; }
        ILabRoomRepository LabRooms { get; }
        IEquipmentRepository Equipments { get; }
        IPriorityRuleRepository PriorityRules { get; }
        IBookingRepository Bookings { get; }
        IBookingItemRepository BookingItems { get; }
        IUsageLogRepository UsageLogs { get; }
        IMaintenanceRepository Maintenances { get; }
        IWaitlistRepository Waitlists { get; }
        IViolationRepository Violations { get; }
        IAuditLogRepository AuditLogs { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        INotificationRepository Notifications { get; }

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        Task ExecuteInSerializableTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }


}
