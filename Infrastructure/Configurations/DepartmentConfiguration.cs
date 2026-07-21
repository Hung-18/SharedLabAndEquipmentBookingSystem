using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> entity)
        {
            entity.ToTable("Departments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Departments_Status",
                    ConfigurationHelpers.EnumCheck<DepartmentStatus>(nameof(Department.Status)));
            });

            entity.HasKey(x => x.DepartmentId);

            entity.Property(x => x.DepartmentName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.DepartmentName)
                .IsUnique();
        }
    }
}
