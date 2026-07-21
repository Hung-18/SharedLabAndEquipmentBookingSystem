using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class LabRoomConfiguration : IEntityTypeConfiguration<LabRoom>
    {
        public void Configure(EntityTypeBuilder<LabRoom> entity)
        {
            entity.ToTable("LabRooms", table =>
            {
                table.HasCheckConstraint(
                    "CK_LabRooms_Status",
                    ConfigurationHelpers.EnumCheck<LabRoomStatus>(nameof(LabRoom.Status)));

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
        }
    }
}
