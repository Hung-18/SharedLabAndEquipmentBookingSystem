using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
    {
        public void Configure(EntityTypeBuilder<Equipment> entity)
        {
            entity.ToTable("Equipments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Equipments_Status",
                    ConfigurationHelpers.EnumCheck<EquipmentStatus>(nameof(Equipment.Status)));
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
        }
    }
}
