using Domain;
using Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AppDbContext
{
    public class ApplicationDbContext : DbContext
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureRole(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureLabRoom(modelBuilder);
            ConfigureEquipment(modelBuilder);
            ConfigurePriorityRule(modelBuilder);
            ConfigureBooking(modelBuilder);
            ConfigureBookingItem(modelBuilder);
            ConfigureUsageLog(modelBuilder);
            ConfigureMaintenance(modelBuilder);
            ConfigureWaitlist(modelBuilder);
            ConfigureViolation(modelBuilder);
            ConfigureAuditLog(modelBuilder);
            ConfigureRefreshToken(modelBuilder);
            ConfigureNotification(modelBuilder);

            SeedData(modelBuilder);
        }

        private static void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasKey(x => x.RoleId);

                entity.Property(x => x.RoleName)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(255);

                entity.HasIndex(x => x.RoleName)
                    .IsUnique();
            });
        }

        private static void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments");

                entity.HasKey(x => x.DepartmentId);

                entity.Property(x => x.DepartmentName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.DepartmentName)
                    .IsUnique();
            });
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(x => x.UserId);

                entity.Property(x => x.FullName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Username)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.PenaltyPoints)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.Username)
                    .IsUnique();

                entity.HasIndex(x => x.Email)
                    .IsUnique();

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Department)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureLabRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LabRoom>(entity =>
            {
                entity.ToTable("LabRooms");

                entity.HasKey(x => x.LabId);

                entity.Property(x => x.LabName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.RoomCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Location)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.Capacity)
                    .IsRequired();

                entity.Property(x => x.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(x => x.UsageGuideline)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.RoomCode)
                    .IsUnique();

                entity.HasOne(x => x.Manager)
                    .WithMany(x => x.ManagedLabRooms)
                    .HasForeignKey(x => x.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureEquipment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.ToTable("Equipments");

                entity.HasKey(x => x.EquipmentId);

                entity.Property(x => x.EquipmentName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.ModelSpecs)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(x => x.UsageGuideline)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasOne(x => x.LabRoom)
                    .WithMany(x => x.Equipments)
                    .HasForeignKey(x => x.LabId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigurePriorityRule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PriorityRule>(entity =>
            {
                entity.ToTable("PriorityRules");

                entity.HasKey(x => x.PriorityRuleId);

                entity.Property(x => x.PurposeType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PriorityLevel)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.PurposeType)
                    .IsUnique();
            });
        }

        private static void ConfigureBooking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");

                entity.HasKey(x => x.BookingId);

                entity.Property(x => x.PurposeType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PurposeDescription)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();

                entity.Property(x => x.StartTime)
                    .IsRequired();

                entity.Property(x => x.EndTime)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.RejectionReason)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(x => new { x.UserId, x.StartTime, x.EndTime });

                entity.HasIndex(x => x.Status);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Bookings)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ApprovedBy)
                    .WithMany(x => x.ApprovedBookings)
                    .HasForeignKey(x => x.ApprovedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.PriorityRule)
                    .WithMany(x => x.Bookings)
                    .HasForeignKey(x => x.PriorityRuleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBookingItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookingItem>(entity =>
            {
                entity.ToTable("BookingItems");

                entity.HasKey(x => x.BookingItemId);

                entity.Property(x => x.ResourceType)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.LabId, x.EquipmentId });

                entity.HasOne(x => x.Booking)
                    .WithMany(x => x.BookingItems)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.LabRoom)
                    .WithMany(x => x.BookingItems)
                    .HasForeignKey(x => x.LabId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Equipment)
                    .WithMany(x => x.BookingItems)
                    .HasForeignKey(x => x.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureUsageLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsageLog>(entity =>
            {
                entity.ToTable("UsageLogs");

                entity.HasKey(x => x.LogId);

                entity.Property(x => x.ActualCheckin)
                    .IsRequired();

                entity.Property(x => x.IncidentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.IncidentDescription)
                    .HasColumnType("nvarchar(max)");

                entity.HasOne(x => x.BookingItem)
                    .WithMany(x => x.UsageLogs)
                    .HasForeignKey(x => x.BookingItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureMaintenance(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Maintenance>(entity =>
            {
                entity.ToTable("Maintenances");

                entity.HasKey(x => x.MaintenanceId);

                entity.Property(x => x.StartTime)
                    .IsRequired();

                entity.Property(x => x.EndTime)
                    .IsRequired();

                entity.Property(x => x.MaintenanceCost)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Notes)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => new { x.LabId, x.StartTime, x.EndTime });

                entity.HasIndex(x => new { x.EquipmentId, x.StartTime, x.EndTime });

                entity.HasOne(x => x.LabRoom)
                    .WithMany(x => x.Maintenances)
                    .HasForeignKey(x => x.LabId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Equipment)
                    .WithMany(x => x.Maintenances)
                    .HasForeignKey(x => x.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedBy)
                    .WithMany(x => x.CreatedMaintenances)
                    .HasForeignKey(x => x.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureWaitlist(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Waitlist>(entity =>
            {
                entity.ToTable("Waitlists");

                entity.HasKey(x => x.WaitlistId);

                entity.Property(x => x.RequestedStart)
                    .IsRequired();

                entity.Property(x => x.RequestedEnd)
                    .IsRequired();

                entity.Property(x => x.QueuePosition)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => new { x.LabId, x.RequestedStart, x.RequestedEnd });

                entity.HasIndex(x => new { x.EquipmentId, x.RequestedStart, x.RequestedEnd });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Waitlists)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LabRoom)
                    .WithMany(x => x.Waitlists)
                    .HasForeignKey(x => x.LabId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Equipment)
                    .WithMany(x => x.Waitlists)
                    .HasForeignKey(x => x.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureViolation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Violation>(entity =>
            {
                entity.ToTable("Violations");

                entity.HasKey(x => x.ViolationId);

                entity.Property(x => x.ViolationType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PenaltyPointsAdded)
                    .IsRequired();

                entity.Property(x => x.LoggedAt)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Violations)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Booking)
                    .WithMany(x => x.Violations)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");

                entity.HasKey(x => x.AuditLogId);

                entity.Property(x => x.ActionType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.EntityName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.OldValue)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.NewValue)
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(50);

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.AuditLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");

                entity.HasKey(x => x.RefreshTokenId);

                entity.Property(x => x.Token)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.Token)
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureNotification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(x => x.NotificationId);

                entity.Property(x => x.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Message)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();

                entity.Property(x => x.NotificationType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.IsRead)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new
                {
                    RoleId = 1,
                    RoleName = RoleName.Admin,
                    Description = "System administrator"
                },
                new
                {
                    RoleId = 2,
                    RoleName = RoleName.LabManager,
                    Description = "Lab room manager"
                },
                new
                {
                    RoleId = 3,
                    RoleName = RoleName.Requester,
                    Description = "User who books lab rooms or equipment"
                }
            );

            modelBuilder.Entity<Department>().HasData(
                new
                {
                    DepartmentId = 1,
                    DepartmentName = "Information Technology",
                    Description = "Department of Information Technology",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 2,
                    DepartmentName = "Electrical and Electronic Engineering",
                    Description = "Department of Electrical and Electronic Engineering",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 3,
                    DepartmentName = "Biology",
                    Description = "Department of Biology",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 4,
                    DepartmentName = "Physics",
                    Description = "Department of Physics",
                    Status = DepartmentStatus.Active
                }
            );

            modelBuilder.Entity<PriorityRule>().HasData(
                new
                {
                    PriorityRuleId = 1,
                    PurposeType = BookingPurposeType.ResearchProject,
                    PriorityLevel = 1,
                    Description = "Highest priority for research projects",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 2,
                    PurposeType = BookingPurposeType.CoursePractice,
                    PriorityLevel = 2,
                    Description = "Priority for course practice sessions",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 3,
                    PurposeType = BookingPurposeType.SelfStudy,
                    PriorityLevel = 3,
                    Description = "Priority for self-study or independent practice",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 4,
                    PurposeType = BookingPurposeType.Other,
                    PriorityLevel = 4,
                    Description = "Other booking purposes",
                    Status = PriorityRuleStatus.Active
                }
            );
        }
    }
}