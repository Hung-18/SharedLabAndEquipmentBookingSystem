using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
    {
        public void Configure(EntityTypeBuilder<Waitlist> entity)
        {
            entity.ToTable("Waitlists", table =>
            {
                table.HasCheckConstraint(
                    "CK_Waitlists_Status",
                    ConfigurationHelpers.EnumCheck<WaitlistStatus>(nameof(Waitlist.Status)));

                table.HasCheckConstraint(
                    "CK_Waitlists_OneResourceOnly",
                    ConfigurationHelpers.SingleResourceCheck);

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
        }
    }
}
