using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> b)
    {
        b.ToTable("areas");
        b.HasKey(a => a.Id);

        b.Property(a => a.Code).IsRequired().HasMaxLength(50);
        b.Property(a => a.Name).IsRequired().HasMaxLength(255);

        // Unique business code among live rows (soft-delete aware).
        b.HasIndex(a => a.Code)
            .IsUnique()
            .HasDatabaseName("ix_areas_code_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(a => a.ParentAreaId).HasDatabaseName("ix_areas_parent_area_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
