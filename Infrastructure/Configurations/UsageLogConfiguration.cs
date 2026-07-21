using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class UsageLogConfiguration : IEntityTypeConfiguration<UsageLog>
    {
        public void Configure(EntityTypeBuilder<UsageLog> entity)
        {
            entity.ToTable("UsageLogs", table =>
            {
                table.HasCheckConstraint(
                    "CK_UsageLogs_IncidentStatus",
                    ConfigurationHelpers.EnumCheck<UsageIncidentStatus>(nameof(UsageLog.IncidentStatus)));

                table.HasCheckConstraint(
                    "CK_UsageLogs_IncidentReviewStatus",
                    ConfigurationHelpers.EnumCheck<IncidentReviewStatus>(nameof(UsageLog.IncidentReviewStatus)));

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

            entity.Property(x => x.IncidentReviewStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.IncidentReviewNote)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.AffectedEquipmentId);
            entity.HasIndex(x => x.IncidentReviewedById);

            entity.HasIndex(x => x.BookingItemId)
                .IsUnique()
                .HasFilter("[ActualCheckout] IS NULL")
                .HasDatabaseName("UX_UsageLogs_OneOpenPerBookingItem");

            entity.HasOne(x => x.BookingItem)
                .WithMany(x => x.UsageLogs)
                .HasForeignKey(x => x.BookingItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.AffectedEquipment)
                .WithMany()
                .HasForeignKey(x => x.AffectedEquipmentId)
                .OnDelete(DeleteBehavior.Restrict);


            entity.HasOne(x => x.IncidentReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.IncidentReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
