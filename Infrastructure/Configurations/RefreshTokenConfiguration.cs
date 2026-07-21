using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> entity)
        {
            entity.ToTable("RefreshTokens", table =>
            {
                table.HasCheckConstraint(
                    "CK_RefreshTokens_Status",
                    ConfigurationHelpers.EnumCheck<RefreshTokenStatus>(nameof(RefreshToken.Status)));

                table.HasCheckConstraint(
                    "CK_RefreshTokens_ExpiresAt_CreatedAt",
                    "[ExpiresAt] > [CreatedAt]");
            });

            entity.HasKey(x => x.RefreshTokenId);

            entity.Property(x => x.Token)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ExpiresAt)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
