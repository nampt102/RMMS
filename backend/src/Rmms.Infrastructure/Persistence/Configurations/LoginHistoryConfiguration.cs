using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Auth;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> b)
    {
        b.ToTable("login_history");
        b.HasKey(l => l.Id);

        b.Property(l => l.UserId).IsRequired();
        b.Property(l => l.DeviceId);
        b.Property(l => l.Success).IsRequired();
        b.Property(l => l.FailureReason).HasMaxLength(100);
        b.Property(l => l.UserAgent).HasMaxLength(500).IsRequired();

        // Npgsql maps System.Net.IPAddress ↔ Postgres `inet` natively. No converter needed.
        b.Property(l => l.IpAddress)
            .HasColumnType("inet");

        // "Recent logins for user" query.
        b.HasIndex(l => new { l.UserId, l.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_login_history_user_created_at_desc");
    }
}
