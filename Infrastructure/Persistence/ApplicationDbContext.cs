using Domain.Entities;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AppDbContext
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LabRoom> LabRooms => Set<LabRoom>();
        public DbSet<Equipment> Equipments => Set<Equipment>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingItem> BookingItems => Set<BookingItem>();
        public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
        public DbSet<Maintenance> Maintenances => Set<Maintenance>();
        public DbSet<Waitlist> Waitlists => Set<Waitlist>();
        public DbSet<Violation> Violations => Set<Violation>();
        public DbSet<PriorityRule> PriorityRules => Set<PriorityRule>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
            modelBuilder.ApplyConfiguration(new LabRoomConfiguration());
            modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
            modelBuilder.ApplyConfiguration(new PriorityRuleConfiguration());
            modelBuilder.ApplyConfiguration(new BookingConfiguration());
            modelBuilder.ApplyConfiguration(new BookingItemConfiguration());
            modelBuilder.ApplyConfiguration(new UsageLogConfiguration());
            modelBuilder.ApplyConfiguration(new MaintenanceConfiguration());
            modelBuilder.ApplyConfiguration(new WaitlistConfiguration());
            modelBuilder.ApplyConfiguration(new ViolationConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());

            SeedDataConfiguration.Apply(modelBuilder);
        }
    }
}
