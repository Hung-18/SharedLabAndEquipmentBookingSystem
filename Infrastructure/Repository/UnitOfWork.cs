using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IRoleRepository? _roles;
        private IDepartmentRepository? _departments;
        private IUserRepository? _users;
        private ILabRoomRepository? _labRooms;
        private IEquipmentRepository? _equipments;
        private IPriorityRuleRepository? _priorityRules;
        private IBookingRepository? _bookings;
        private IBookingItemRepository? _bookingItems;
        private IUsageLogRepository? _usageLogs;
        private IMaintenanceRepository? _maintenances;
        private IWaitlistRepository? _waitlists;
        private IViolationRepository? _violations;
        private IAuditLogRepository? _auditLogs;
        private IRefreshTokenRepository? _refreshTokens;
        private INotificationRepository? _notifications;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
        public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(_context);
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public ILabRoomRepository LabRooms => _labRooms ??= new LabRoomRepository(_context);
        public IEquipmentRepository Equipments => _equipments ??= new EquipmentRepository(_context);
        public IPriorityRuleRepository PriorityRules => _priorityRules ??= new PriorityRuleRepository(_context);
        public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context);
        public IBookingItemRepository BookingItems => _bookingItems ??= new BookingItemRepository(_context);
        public IUsageLogRepository UsageLogs => _usageLogs ??= new UsageLogRepository(_context);
        public IMaintenanceRepository Maintenances => _maintenances ??= new MaintenanceRepository(_context);
        public IWaitlistRepository Waitlists => _waitlists ??= new WaitlistRepository(_context);
        public IViolationRepository Violations => _violations ??= new ViolationRepository(_context);
        public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
        public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            return ExecuteTransactionCoreAsync(
                operation,
                IsolationLevel.ReadCommitted,
                cancellationToken);
        }

        public Task ExecuteInSerializableTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            return ExecuteTransactionCoreAsync(
                operation,
                IsolationLevel.Serializable,
                cancellationToken);
        }

        private async Task ExecuteTransactionCoreAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    isolationLevel,
                    cancellationToken);

            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
