using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories");
        b.HasKey(c => c.Id);

        b.Property(c => c.Code).IsRequired().HasMaxLength(50);
        b.Property(c => c.Name).IsRequired().HasMaxLength(255);

        b.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("ix_categories_code_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(c => c.DeletedAt == null);
    }
}
