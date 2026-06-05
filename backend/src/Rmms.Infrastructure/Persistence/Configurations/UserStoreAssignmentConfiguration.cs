using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class UserStoreAssignmentConfiguration : IEntityTypeConfiguration<UserStoreAssignment>
{
    public void Configure(EntityTypeBuilder<UserStoreAssignment> b)
    {
        b.ToTable("user_store_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.UserId).IsRequired();
        b.Property(a => a.StoreId).IsRequired();
        b.Property(a => a.EffectiveFrom).IsRequired();
        b.Property(a => a.EffectiveTo);

        // A user may have many active store assignments — prevent only DUPLICATE
        // open-ended links to the same store.
        b.HasIndex(a => new { a.UserId, a.StoreId })
            .IsUnique()
            .HasDatabaseName("ix_user_store_active_unique")
            .HasFilter("effective_to IS NULL AND deleted_at IS NULL");

        b.HasIndex(a => a.StoreId).HasDatabaseName("ix_user_store_store_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
