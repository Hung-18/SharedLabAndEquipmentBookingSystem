using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class MaintenanceConfiguration : IEntityTypeConfiguration<Maintenance>
    {
        public void Configure(EntityTypeBuilder<Maintenance> entity)
        {
            entity.ToTable("Maintenances", table =>
            {
                table.HasCheckConstraint(
                    "CK_Maintenances_Status",
                    ConfigurationHelpers.EnumCheck<MaintenanceStatus>(nameof(Maintenance.Status)));

                table.HasCheckConstraint(
                    "CK_Maintenances_OneResourceOnly",
                    ConfigurationHelpers.SingleResourceCheck);

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

            entity.Property(x => x.RecurrenceStopped)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(x => x.PreviousResourceStatus);

            entity.HasIndex(x => new
            {
                x.ParentMaintenanceId,
                x.StartTime
            })
                .IsUnique()
                .HasFilter("[ParentMaintenanceId] IS NOT NULL")
                .HasDatabaseName(
                    "UX_Maintenances_Parent_StartTime");

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
        }
    }
}
