using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Application.Common;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> b)
    {
        b.ToTable("approvals");
        b.HasKey(a => a.Id);

        b.Property(a => a.EntityType)
            .HasConversion(v => v.ToSnakeCase(), v => FromSnake<ApprovalEntityType>(v))
            .HasMaxLength(50)
            .IsRequired();

        b.Property(a => a.EntityId).IsRequired();
        b.Property(a => a.RequesterId).IsRequired();
        b.Property(a => a.ApproverId).IsRequired();

        b.Property(a => a.ApproverRole)
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<UserRole>(v, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();

        b.Property(a => a.Status)
            .HasConversion(v => v.ToSnakeCase(), v => FromSnake<ApprovalStatus>(v))
            .HasMaxLength(20)
            .IsRequired();

        b.Property(a => a.DecisionReason).HasColumnType("text");

        b.Property(a => a.DecidedVia)
            .HasConversion(
                v => v == null ? null : v.Value.ToSnakeCase(),
                v => v == null ? (ApprovalDecisionVia?)null : FromSnake<ApprovalDecisionVia>(v))
            .HasMaxLength(20);

        b.Property(a => a.OverrideReason).HasColumnType("text");

        b.HasQueryFilter(a => a.DeletedAt == null);

        // Approver's queue (pending list) + requester history.
        b.HasIndex(a => new { a.ApproverId, a.Status })
            .HasDatabaseName("ix_approvals_approver_status");
        b.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("ix_approvals_entity");
        b.HasIndex(a => a.RequesterId)
            .HasDatabaseName("ix_approvals_requester");
    }

    private static T FromSnake<T>(string v) where T : struct, Enum =>
        Enum.Parse<T>(v.Replace("_", string.Empty), ignoreCase: true);
}
