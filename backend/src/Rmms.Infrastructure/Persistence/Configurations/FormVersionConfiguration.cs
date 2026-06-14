using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Forms;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class FormVersionConfiguration : IEntityTypeConfiguration<FormVersion>
{
    public void Configure(EntityTypeBuilder<FormVersion> b)
    {
        b.ToTable("form_versions");
        b.HasKey(v => v.Id);

        b.Property(v => v.FormId).IsRequired();
        b.Property(v => v.Version).IsRequired();
        b.Property(v => v.Schema).HasColumnType("jsonb").IsRequired();
        b.Property(v => v.PublishedAt);
        b.Property(v => v.PublishedBy);

        // One version number per form.
        b.HasIndex(v => new { v.FormId, v.Version })
            .IsUnique()
            .HasDatabaseName("ix_form_versions_form_version_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(v => v.DeletedAt == null);
    }
}
