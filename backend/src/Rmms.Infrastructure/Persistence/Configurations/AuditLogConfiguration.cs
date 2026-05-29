using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Audit;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log");
        b.HasKey(a => a.Id);

        b.Property(a => a.ActorUserId);
        b.Property(a => a.Action).IsRequired().HasMaxLength(80);
        b.Property(a => a.TargetEntity).IsRequired().HasMaxLength(50);
        b.Property(a => a.TargetId);
        b.Property(a => a.UserAgent).HasMaxLength(500).IsRequired();

        // Npgsql maps System.Net.IPAddress ↔ Postgres `inet` natively. No converter needed.
        b.Property(a => a.IpAddress)
            .HasColumnType("inet");

        // jsonb for flexible per-module context (per CR-1 / sprint-01.md §9.4).
        b.Property(a => a.Metadata)
            .HasColumnType("jsonb")
            .IsRequired();

        // Compliance / Admin investigation indexes.
        b.HasIndex(a => new { a.ActorUserId, a.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_log_actor_created_at_desc");

        b.HasIndex(a => new { a.TargetEntity, a.TargetId, a.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_audit_log_target_created_at_desc");

        b.HasIndex(a => a.Action)
            .HasDatabaseName("ix_audit_log_action");

        // Note: append-only ENFORCEMENT (REVOKE UPDATE, DELETE) lives in the
        // accompanying raw-SQL migration step, not here, because EF Core has no
        // primitives for role grants.
    }
}
