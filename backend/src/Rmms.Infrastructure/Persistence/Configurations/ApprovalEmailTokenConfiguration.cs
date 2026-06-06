using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Approvals;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalEmailTokenConfiguration : IEntityTypeConfiguration<ApprovalEmailToken>
{
    public void Configure(EntityTypeBuilder<ApprovalEmailToken> b)
    {
        b.ToTable("approval_email_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.ApprovalId).IsRequired();
        b.Property(t => t.TokenHash).HasMaxLength(255).IsRequired();
        b.Property(t => t.ExpiresAt).IsRequired();
        b.Property(t => t.IpAddress).HasMaxLength(64);
        b.Property(t => t.UserAgent).HasColumnType("text");

        // Lookup by hash on consume (one-time-use check).
        b.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_approval_email_tokens_hash");
        b.HasIndex(t => t.ApprovalId)
            .HasDatabaseName("ix_approval_email_tokens_approval");
    }
}
