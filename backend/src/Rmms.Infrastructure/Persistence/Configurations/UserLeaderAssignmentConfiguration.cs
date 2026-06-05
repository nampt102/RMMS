using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class UserLeaderAssignmentConfiguration : IEntityTypeConfiguration<UserLeaderAssignment>
{
    public void Configure(EntityTypeBuilder<UserLeaderAssignment> b)
    {
        b.ToTable("user_leader_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.PgUserId).IsRequired();
        b.Property(a => a.LeaderUserId).IsRequired();
        b.Property(a => a.EffectiveFrom).IsRequired();
        b.Property(a => a.EffectiveTo);

        // BR/M03: a PG has at most ONE active (open-ended) Leader at a time.
        b.HasIndex(a => a.PgUserId)
            .IsUnique()
            .HasDatabaseName("ix_user_leader_one_active_per_pg")
            .HasFilter("effective_to IS NULL AND deleted_at IS NULL");

        b.HasIndex(a => a.LeaderUserId).HasDatabaseName("ix_user_leader_leader_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
