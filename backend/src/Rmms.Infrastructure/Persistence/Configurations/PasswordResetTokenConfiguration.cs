using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Auth;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("password_reset_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.UserId).IsRequired();
        b.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        b.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_password_reset_tokens_hash_unique");

        b.HasIndex(t => new { t.UserId, t.UsedAt })
            .HasDatabaseName("ix_password_reset_tokens_user_used");

        b.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("ix_password_reset_tokens_expires_at");
    }
}
