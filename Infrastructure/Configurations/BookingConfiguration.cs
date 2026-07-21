using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> entity)
        {
            entity.ToTable("Bookings", table =>
            {
                table.HasCheckConstraint(
                    "CK_Bookings_PurposeType",
                    ConfigurationHelpers.EnumCheck<BookingPurposeType>(nameof(Booking.PurposeType)));

                table.HasCheckConstraint(
                    "CK_Bookings_Status",
                    ConfigurationHelpers.EnumCheck<BookingStatus>(nameof(Booking.Status)));

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
        }
    }
}
