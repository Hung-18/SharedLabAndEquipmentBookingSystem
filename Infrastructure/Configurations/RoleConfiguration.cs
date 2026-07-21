using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> entity)
        {
            entity.ToTable("Roles", table =>
            {
                table.HasCheckConstraint(
                    "CK_Roles_RoleName",
                    ConfigurationHelpers.EnumCheck<RoleName>(nameof(Role.RoleName)));
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
        }
    }
}
