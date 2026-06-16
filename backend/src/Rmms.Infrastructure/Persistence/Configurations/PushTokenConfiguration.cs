using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Notifications;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
{
    public void Configure(EntityTypeBuilder<PushToken> b)
    {
        b.ToTable("push_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.UserId).IsRequired();
        b.Property(t => t.Token).HasMaxLength(512).IsRequired();

        // One row per FCM token (the install). Lets register re-point the token to the
        // account currently signed in on that device.
        b.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("ix_push_tokens_token_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(t => t.UserId).HasDatabaseName("ix_push_tokens_user");

        b.HasQueryFilter(t => t.DeletedAt == null);
    }
}
