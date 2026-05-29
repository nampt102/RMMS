using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Auth;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.UserId).IsRequired();
        b.Property(t => t.DeviceId).IsRequired();

        b.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex = 64 chars

        // Lookup by hash on /auth/refresh.
        b.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_hash_unique");

        // "Active tokens for user/device" query.
        b.HasIndex(t => new { t.UserId, t.DeviceId, t.RevokedAt })
            .HasDatabaseName("ix_refresh_tokens_user_device_revoked");

        // Hangfire cleanup job filter.
        b.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");
    }
}
