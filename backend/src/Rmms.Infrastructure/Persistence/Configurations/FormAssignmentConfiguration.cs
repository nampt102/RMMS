using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Forms;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class FormAssignmentConfiguration : IEntityTypeConfiguration<FormAssignment>
{
    public void Configure(EntityTypeBuilder<FormAssignment> b)
    {
        b.ToTable("form_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.FormId).IsRequired();
        b.Property(a => a.AssignedToRole).HasMaxLength(20);
        b.Property(a => a.ValidFrom).IsRequired();

        b.HasIndex(a => a.FormId).HasDatabaseName("ix_form_assignments_form_id");
        b.HasIndex(a => a.AssignedToRole).HasDatabaseName("ix_form_assignments_role");
        b.HasIndex(a => a.AssignedToUserId).HasDatabaseName("ix_form_assignments_user");
        b.HasIndex(a => a.AssignedToStoreId).HasDatabaseName("ix_form_assignments_store");
        b.HasIndex(a => a.AssignedToCategoryId).HasDatabaseName("ix_form_assignments_category");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
