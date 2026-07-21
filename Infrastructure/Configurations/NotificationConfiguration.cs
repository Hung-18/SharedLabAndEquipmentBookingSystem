using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> entity)
        {
            entity.ToTable("Notifications", table =>
            {
                table.HasCheckConstraint(
                    "CK_Notifications_NotificationType",
                    ConfigurationHelpers.EnumCheck<NotificationType>(nameof(Notification.NotificationType)));
            });

            entity.HasKey(x => x.NotificationId);

            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Message)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(x => x.NotificationType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.IsRead)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
