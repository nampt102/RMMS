using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class UserCategoryAssignmentConfiguration : IEntityTypeConfiguration<UserCategoryAssignment>
{
    public void Configure(EntityTypeBuilder<UserCategoryAssignment> b)
    {
        b.ToTable("user_category_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.UserId).IsRequired();
        b.Property(a => a.CategoryId).IsRequired();

        // A user is linked to a category at most once.
        b.HasIndex(a => new { a.UserId, a.CategoryId })
            .IsUnique()
            .HasDatabaseName("ix_user_category_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(a => a.CategoryId).HasDatabaseName("ix_user_category_category_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
