using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class BookingItemConfiguration : IEntityTypeConfiguration<BookingItem>
    {
        public void Configure(EntityTypeBuilder<BookingItem> entity)
        {
            entity.ToTable("BookingItems", table =>
            {
                table.HasCheckConstraint(
                    "CK_BookingItems_ResourceType",
                    ConfigurationHelpers.EnumCheck<ResourceType>(nameof(BookingItem.ResourceType)));

                table.HasCheckConstraint(
                    "CK_BookingItems_OneResourceOnly",
                    ConfigurationHelpers.BookingItemResourceCheck);

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
        }
    }
}
