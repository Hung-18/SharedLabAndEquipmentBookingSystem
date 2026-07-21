using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class PriorityRuleConfiguration : IEntityTypeConfiguration<PriorityRule>
    {
        public void Configure(EntityTypeBuilder<PriorityRule> entity)
        {
            entity.ToTable("PriorityRules", table =>
            {
                table.HasCheckConstraint(
                    "CK_PriorityRules_PurposeType",
                    ConfigurationHelpers.EnumCheck<BookingPurposeType>(nameof(PriorityRule.PurposeType)));

                table.HasCheckConstraint(
                    "CK_PriorityRules_Status",
                    ConfigurationHelpers.EnumCheck<PriorityRuleStatus>(nameof(PriorityRule.Status)));

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
        }
    }
}
