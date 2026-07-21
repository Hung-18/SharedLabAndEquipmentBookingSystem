using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ViolationConfiguration : IEntityTypeConfiguration<Violation>
    {
        public void Configure(EntityTypeBuilder<Violation> entity)
        {
            entity.ToTable("Violations", table =>
            {
                table.HasCheckConstraint(
                    "CK_Violations_ViolationType",
                    ConfigurationHelpers.EnumCheck<ViolationType>(nameof(Violation.ViolationType)));

                table.HasCheckConstraint(
                    "CK_Violations_Status",
                    ConfigurationHelpers.EnumCheck<ViolationStatus>(nameof(Violation.Status)));

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
        }
    }
}
