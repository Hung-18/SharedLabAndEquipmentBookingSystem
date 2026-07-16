using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

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
            ConfigurePasswordResetToken(modelBuilder);

            SeedData(modelBuilder);
        }

        // Call this once after Database.Migrate() in Program.cs.
        // Reason: HasTrigger only tells EF Core that a table has a trigger.
        // It does NOT create the SQL trigger in database.
        public async Task EnsureDatabaseGuardsCreatedAsync(CancellationToken cancellationToken = default)
        {
            await Database.ExecuteSqlRawAsync(CreateOpenUsageLogUniqueIndexSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateActiveViolationUniqueIndexSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateActiveWaitlistUniqueIndexesSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterBookingItemsPreventConflictTriggerSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterBookingsPreventConflictTriggerSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterMaintenancesPreventConflictTriggerSql, cancellationToken);
        }

        // App-level guard before creating/updating a booking item.
        // It checks both booking-vs-booking and booking-vs-maintenance.
        public Task<bool> HasLabScheduleConflictAsync(
            int labId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId = null,
            CancellationToken cancellationToken = default)
        {
            if (labId <= 0)
                throw new ArgumentException("LabId must be greater than 0", nameof(labId));

            return HasResourceScheduleConflictAsync(
                labId: labId,
                equipmentId: null,
                startTime: startTime,
                endTime: endTime,
                excludedBookingId: excludedBookingId,
                cancellationToken: cancellationToken);
        }

        // App-level guard before creating/updating a booking item.
        // It checks both booking-vs-booking and booking-vs-maintenance.
        public Task<bool> HasEquipmentScheduleConflictAsync(
            int equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId = null,
            CancellationToken cancellationToken = default)
        {
            if (equipmentId <= 0)
                throw new ArgumentException("EquipmentId must be greater than 0", nameof(equipmentId));

            return HasResourceScheduleConflictAsync(
                labId: null,
                equipmentId: equipmentId,
                startTime: startTime,
                endTime: endTime,
                excludedBookingId: excludedBookingId,
                cancellationToken: cancellationToken);
        }

        private async Task<bool> HasResourceScheduleConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId,
            CancellationToken cancellationToken)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Start time must be earlier than end time");

            if ((labId.HasValue && equipmentId.HasValue) || (!labId.HasValue && !equipmentId.HasValue))
                throw new ArgumentException("Exactly one resource must be selected");

            var activeBookingStatuses = new[]
            {
                BookingStatus.Approved
            };

            var activeMaintenanceStatuses = new[]
            {
                MaintenanceStatus.Scheduled,
                MaintenanceStatus.InProgress
            };

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                equipmentLabId = await Equipments
                    .Where(x => x.EquipmentId == equipmentId.Value)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var bookingConflict = await BookingItems.AnyAsync(x =>
                x.Booking != null
                && activeBookingStatuses.Contains(x.Booking.Status)
                && x.Booking.StartTime < endTime
                && x.Booking.EndTime > startTime
                && (!excludedBookingId.HasValue || x.BookingId != excludedBookingId.Value)
                && (
                    (labId.HasValue
                        && (x.LabId == labId.Value
                            || (x.Equipment != null && x.Equipment.LabId == labId.Value)))
                    ||
                    (equipmentId.HasValue
                        && (x.EquipmentId == equipmentId.Value
                            || (equipmentLabId.HasValue && x.LabId == equipmentLabId.Value)))
                ),
                cancellationToken);

            if (bookingConflict)
                return true;

            var maintenanceConflict = await Maintenances.AnyAsync(x =>
                activeMaintenanceStatuses.Contains(x.Status) &&
                x.StartTime < endTime &&
                x.EndTime > startTime &&
                (
                    (labId.HasValue && x.LabId == labId.Value) ||
                    (equipmentId.HasValue &&
                        (x.EquipmentId == equipmentId.Value ||
                         (equipmentLabId.HasValue && x.LabId == equipmentLabId.Value)))
                ),
                cancellationToken);

            return maintenanceConflict;
        }

        private static string EnumCheck<TEnum>(string columnName)
            where TEnum : struct, Enum
        {
            var values = string.Join(", ", Enum.GetNames<TEnum>().Select(x => $"'{x}'"));
            return $"[{columnName}] IN ({values})";
        }

        // Accept both 'LabRoom' and 'Lab' to avoid mismatch if your ResourceType enum is named Lab instead of LabRoom.
        private const string BookingItemResourceCheck = @"
(
    ([ResourceType] IN ('LabRoom', 'Lab') AND [LabId] IS NOT NULL AND [EquipmentId] IS NULL)
    OR
    ([ResourceType] = 'Equipment' AND [LabId] IS NULL AND [EquipmentId] IS NOT NULL)
)";

        private const string SingleResourceCheck = @"
(
    ([LabId] IS NOT NULL AND [EquipmentId] IS NULL)
    OR
    ([LabId] IS NULL AND [EquipmentId] IS NOT NULL)
)";

        private static void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Roles_RoleName",
                        EnumCheck<RoleName>(nameof(Role.RoleName)));
                });

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
                entity.ToTable("Departments", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Departments_Status",
                        EnumCheck<DepartmentStatus>(nameof(Department.Status)));
                });

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
                entity.ToTable("Users", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Users_Status",
                        EnumCheck<UserStatus>(nameof(User.Status)));

                    table.HasCheckConstraint(
                        "CK_Users_PenaltyPoints",
                        "[PenaltyPoints] >= 0");
                });

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

        private static void ConfigurePasswordResetToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens");
                entity.HasKey(x => x.TokenId);
                entity.Property(x => x.Email)
                    .HasMaxLength(150)
                    .IsRequired();
                entity.Property(x => x.Token)
                    .HasMaxLength(500)
                    .IsRequired();
                entity.Property(x => x.ExpiryDate)
                    .IsRequired();
                entity.HasIndex(x => x.Token)
                    .IsUnique();
            });
        }

        private static void ConfigureLabRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LabRoom>(entity =>
            {
                entity.ToTable("LabRooms", table =>
                {
                    table.HasCheckConstraint(
                        "CK_LabRooms_Status",
                        EnumCheck<LabRoomStatus>(nameof(LabRoom.Status)));

                    table.HasCheckConstraint(
                        "CK_LabRooms_Capacity",
                        "[Capacity] > 0");
                });

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

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

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
                entity.ToTable("Equipments", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Equipments_Status",
                        EnumCheck<EquipmentStatus>(nameof(Equipment.Status)));
                });

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
                entity.ToTable("PriorityRules", table =>
                {
                    table.HasCheckConstraint(
                        "CK_PriorityRules_PurposeType",
                        EnumCheck<BookingPurposeType>(nameof(PriorityRule.PurposeType)));

                    table.HasCheckConstraint(
                        "CK_PriorityRules_Status",
                        EnumCheck<PriorityRuleStatus>(nameof(PriorityRule.Status)));

                    table.HasCheckConstraint(
                        "CK_PriorityRules_PriorityLevel",
                        "[PriorityLevel] > 0");
                });

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
                entity.ToTable("Bookings", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Bookings_PurposeType",
                        EnumCheck<BookingPurposeType>(nameof(Booking.PurposeType)));

                    table.HasCheckConstraint(
                        "CK_Bookings_Status",
                        EnumCheck<BookingStatus>(nameof(Booking.Status)));

                    table.HasCheckConstraint(
                        "CK_Bookings_StartTime_EndTime",
                        "[StartTime] < [EndTime]");

                    table.HasTrigger("TRG_Bookings_PreventConflict");
                });

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

                entity.HasIndex(x => new { x.StartTime, x.EndTime });
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
                entity.ToTable("BookingItems", table =>
                {
                    table.HasCheckConstraint(
                        "CK_BookingItems_ResourceType",
                        EnumCheck<ResourceType>(nameof(BookingItem.ResourceType)));

                    table.HasCheckConstraint(
                        "CK_BookingItems_OneResourceOnly",
                        BookingItemResourceCheck);

                    table.HasTrigger("TRG_BookingItems_PreventConflict");
                });

                entity.HasKey(x => x.BookingItemId);

                entity.Property(x => x.ResourceType)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Note)
                    .HasMaxLength(500);

                entity.HasIndex(x => x.BookingId);
                entity.HasIndex(x => x.LabId);
                entity.HasIndex(x => x.EquipmentId);

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
                entity.ToTable("UsageLogs", table =>
                {
                    table.HasCheckConstraint(
                        "CK_UsageLogs_IncidentStatus",
                        EnumCheck<UsageIncidentStatus>(nameof(UsageLog.IncidentStatus)));

                    table.HasCheckConstraint(
                        "CK_UsageLogs_Checkin_Checkout",
                        "[ActualCheckout] IS NULL OR [ActualCheckin] <= [ActualCheckout]");
                });

                entity.HasKey(x => x.LogId);

                entity.Property(x => x.ActualCheckin)
                    .IsRequired();

                entity.Property(x => x.IncidentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.IncidentDescription)
                    .HasColumnType("nvarchar(max)");

                entity.HasIndex(x => x.BookingItemId)
                    .IsUnique()
                    .HasFilter("[ActualCheckout] IS NULL")
                    .HasDatabaseName("UX_UsageLogs_OneOpenPerBookingItem");

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
                entity.ToTable("Maintenances", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Maintenances_Status",
                        EnumCheck<MaintenanceStatus>(nameof(Maintenance.Status)));

                    table.HasCheckConstraint(
                        "CK_Maintenances_OneResourceOnly",
                        SingleResourceCheck);

                    table.HasCheckConstraint(
                        "CK_Maintenances_StartTime_EndTime",
                        "[StartTime] < [EndTime]");

                    table.HasCheckConstraint(
                        "CK_Maintenances_MaintenanceCost",
                        "[MaintenanceCost] >= 0");

                    table.HasTrigger("TRG_Maintenances_PreventConflict");
                });

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

                entity.Property(x => x.RecurrenceType)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.RecurrenceInterval)
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
                entity.ToTable("Waitlists", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Waitlists_Status",
                        EnumCheck<WaitlistStatus>(nameof(Waitlist.Status)));

                    table.HasCheckConstraint(
                        "CK_Waitlists_OneResourceOnly",
                        SingleResourceCheck);

                    table.HasCheckConstraint(
                        "CK_Waitlists_RequestedStart_RequestedEnd",
                        "[RequestedStart] < [RequestedEnd]");

                    table.HasCheckConstraint(
                        "CK_Waitlists_QueuePosition",
                        "[QueuePosition] > 0");
                });

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
                entity.ToTable("Violations", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Violations_ViolationType",
                        EnumCheck<ViolationType>(nameof(Violation.ViolationType)));

                    table.HasCheckConstraint(
                        "CK_Violations_Status",
                        EnumCheck<ViolationStatus>(nameof(Violation.Status)));

                    table.HasCheckConstraint(
                        "CK_Violations_PenaltyPointsAdded",
                        "[PenaltyPointsAdded] > 0");
                });

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

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.BookingId,
                    x.ViolationType
                })
                    .IsUnique()
                    .HasFilter("[Status] = 'Active'")
                    .HasDatabaseName("UX_Violations_OneActivePerBookingType");

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
                entity.ToTable("AuditLogs", table =>
                {
                    table.HasCheckConstraint(
                        "CK_AuditLogs_ActionType",
                        EnumCheck<AuditActionType>(nameof(AuditLog.ActionType)));
                });

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
                entity.ToTable("RefreshTokens", table =>
                {
                    table.HasCheckConstraint(
                        "CK_RefreshTokens_Status",
                        EnumCheck<RefreshTokenStatus>(nameof(RefreshToken.Status)));

                    table.HasCheckConstraint(
                        "CK_RefreshTokens_ExpiresAt_CreatedAt",
                        "[ExpiresAt] > [CreatedAt]");
                });

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
                entity.ToTable("Notifications", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Notifications_NotificationType",
                        EnumCheck<NotificationType>(nameof(Notification.NotificationType)));
                });

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

        private const string CreateOpenUsageLogUniqueIndexSql = """
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'UX_UsageLogs_OneOpenPerBookingItem'
      AND [object_id] = OBJECT_ID(N'[UsageLogs]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_UsageLogs_OneOpenPerBookingItem]
        ON [UsageLogs] ([BookingItemId])
        WHERE [ActualCheckout] IS NULL;
END
""";

        private const string CreateActiveViolationUniqueIndexSql = """
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'UX_Violations_OneActivePerBookingType'
      AND [object_id] = OBJECT_ID(N'[Violations]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Violations_OneActivePerBookingType]
        ON [Violations] ([UserId], [BookingId], [ViolationType])
        WHERE [Status] = 'Active';
END
""";

        private const string CreateActiveWaitlistUniqueIndexesSql = """
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = 'UX_Waitlists_ActiveUserResourceSlot'
      AND [object_id] = OBJECT_ID(N'[Waitlists]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Waitlists_ActiveUserResourceSlot]
        ON [Waitlists]
        ([UserId], [LabId], [EquipmentId], [RequestedStart], [RequestedEnd])
        WHERE [Status] IN ('Waiting', 'Notified');
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = 'UX_Waitlists_ActiveQueuePosition'
      AND [object_id] = OBJECT_ID(N'[Waitlists]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Waitlists_ActiveQueuePosition]
        ON [Waitlists]
        ([LabId], [EquipmentId], [RequestedStart], [RequestedEnd], [QueuePosition])
        WHERE [Status] IN ('Waiting', 'Notified');
END;
""";

        private const string CreateOrAlterBookingItemsPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_BookingItems_PreventConflict]
ON [BookingItems]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Pending requests may share a slot, but an existing Approved booking locks the resource.
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN [Bookings] b
            ON b.[BookingId] = i.[BookingId]
        LEFT JOIN [Equipments] insertedEquipment
            ON insertedEquipment.[EquipmentId] = i.[EquipmentId]
        INNER JOIN [BookingItems] otherItem
            ON otherItem.[BookingItemId] <> i.[BookingItemId]
        LEFT JOIN [Equipments] otherEquipment
            ON otherEquipment.[EquipmentId] = otherItem.[EquipmentId]
        INNER JOIN [Bookings] otherBooking
            ON otherBooking.[BookingId] = otherItem.[BookingId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND otherBooking.[Status] = 'Approved'
          AND b.[BookingId] <> otherBooking.[BookingId]
          AND b.[StartTime] < otherBooking.[EndTime]
          AND b.[EndTime] > otherBooking.[StartTime]
          AND
          (
              (i.[LabId] IS NOT NULL AND otherItem.[LabId] = i.[LabId])
              OR
              (i.[EquipmentId] IS NOT NULL
               AND otherItem.[EquipmentId] = i.[EquipmentId])
              OR
              (i.[LabId] IS NOT NULL
               AND otherEquipment.[LabId] = i.[LabId])
              OR
              (insertedEquipment.[LabId] IS NOT NULL
               AND otherItem.[LabId] = insertedEquipment.[LabId])
          )
    )
    BEGIN
        THROW 50001, 'Booking schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN [Bookings] b
            ON b.[BookingId] = i.[BookingId]
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = i.[EquipmentId]
        INNER JOIN [Maintenances] m ON
        (
            (i.[LabId] IS NOT NULL AND m.[LabId] = i.[LabId])
            OR
            (i.[EquipmentId] IS NOT NULL
             AND m.[EquipmentId] = i.[EquipmentId])
            OR
            (i.[EquipmentId] IS NOT NULL
             AND m.[LabId] = itemEquipment.[LabId])
        )
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[StartTime] < m.[EndTime]
          AND b.[EndTime] > m.[StartTime]
    )
    BEGIN
        THROW 50002, 'Booking schedule overlaps with maintenance time.', 1;
    END;
END
""";

        private const string CreateOrAlterBookingsPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_Bookings_PreventConflict]
ON [Bookings]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Approval is the point at which a booking locks a resource.
    IF EXISTS
    (
        SELECT 1
        FROM inserted b
        INNER JOIN [BookingItems] item
            ON item.[BookingId] = b.[BookingId]
        LEFT JOIN [Equipments] itemEquipmentForConflict
            ON itemEquipmentForConflict.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [BookingItems] otherItem
            ON otherItem.[BookingItemId] <> item.[BookingItemId]
        LEFT JOIN [Equipments] otherEquipmentForConflict
            ON otherEquipmentForConflict.[EquipmentId] = otherItem.[EquipmentId]
        INNER JOIN [Bookings] otherBooking
            ON otherBooking.[BookingId] = otherItem.[BookingId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND otherBooking.[Status] = 'Approved'
          AND otherBooking.[BookingId] <> b.[BookingId]
          AND b.[StartTime] < otherBooking.[EndTime]
          AND b.[EndTime] > otherBooking.[StartTime]
          AND
          (
              (item.[LabId] IS NOT NULL
               AND otherItem.[LabId] = item.[LabId])
              OR
              (item.[EquipmentId] IS NOT NULL
               AND otherItem.[EquipmentId] = item.[EquipmentId])
              OR
              (item.[LabId] IS NOT NULL
               AND otherEquipmentForConflict.[LabId] = item.[LabId])
              OR
              (itemEquipmentForConflict.[LabId] IS NOT NULL
               AND otherItem.[LabId] = itemEquipmentForConflict.[LabId])
          )
    )
    BEGIN
        THROW 50003, 'Booking schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted b
        INNER JOIN [BookingItems] item
            ON item.[BookingId] = b.[BookingId]
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [Maintenances] m ON
        (
            (item.[LabId] IS NOT NULL AND m.[LabId] = item.[LabId])
            OR
            (item.[EquipmentId] IS NOT NULL
             AND m.[EquipmentId] = item.[EquipmentId])
            OR
            (item.[EquipmentId] IS NOT NULL
             AND m.[LabId] = itemEquipment.[LabId])
        )
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[StartTime] < m.[EndTime]
          AND b.[EndTime] > m.[StartTime]
    )
    BEGIN
        THROW 50004, 'Booking schedule overlaps with maintenance time.', 1;
    END;
END
""";

        private const string CreateOrAlterMaintenancesPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_Maintenances_PreventConflict]
ON [Maintenances]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted m
        INNER JOIN [BookingItems] item ON 1 = 1
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [Bookings] b
            ON b.[BookingId] = item.[BookingId]
        WHERE m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[Status] = 'Approved'
          AND m.[StartTime] < b.[EndTime]
          AND m.[EndTime] > b.[StartTime]
          AND
          (
              (m.[LabId] IS NOT NULL
               AND (item.[LabId] = m.[LabId]
                    OR itemEquipment.[LabId] = m.[LabId]))
              OR
              (m.[EquipmentId] IS NOT NULL
               AND item.[EquipmentId] = m.[EquipmentId])
          )
    )
    BEGIN
        THROW 50005, 'Maintenance schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted m
        LEFT JOIN [Equipments] insertedEquipment
            ON insertedEquipment.[EquipmentId] = m.[EquipmentId]
        INNER JOIN [Maintenances] otherMaintenance
            ON otherMaintenance.[MaintenanceId] <> m.[MaintenanceId]
        LEFT JOIN [Equipments] otherEquipment
            ON otherEquipment.[EquipmentId] = otherMaintenance.[EquipmentId]
        WHERE m.[Status] IN ('Scheduled', 'InProgress')
          AND otherMaintenance.[Status] IN ('Scheduled', 'InProgress')
          AND m.[StartTime] < otherMaintenance.[EndTime]
          AND m.[EndTime] > otherMaintenance.[StartTime]
          AND
          (
              (m.[LabId] IS NOT NULL
               AND otherMaintenance.[LabId] = m.[LabId])
              OR
              (m.[EquipmentId] IS NOT NULL
               AND otherMaintenance.[EquipmentId] = m.[EquipmentId])
              OR
              (m.[LabId] IS NOT NULL
               AND otherEquipment.[LabId] = m.[LabId])
              OR
              (m.[EquipmentId] IS NOT NULL
               AND otherMaintenance.[LabId] = insertedEquipment.[LabId])
          )
    )
    BEGIN
        THROW 50006, 'Maintenance schedule overlaps with an existing maintenance schedule.', 1;
    END;
END
""";
    }
}
