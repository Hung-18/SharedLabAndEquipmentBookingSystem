using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> entity)
        {
            entity.ToTable("AuditLogs", table =>
            {
                table.HasCheckConstraint(
                    "CK_AuditLogs_ActionType",
                    ConfigurationHelpers.EnumCheck<AuditActionType>(nameof(AuditLog.ActionType)));
            });

            entity.HasKey(x => x.AuditLogId);

            entity.Property(x => x.ActionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.EntityName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.OldValue)
                .HasColumnType("nvarchar(max)");

            entity.Property(x => x.NewValue)
                .HasColumnType("nvarchar(max)");

            entity.Property(x => x.IpAddress)
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
