using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Documents;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.HasKey(d => d.Id);

        b.Property(d => d.Name).HasMaxLength(255).IsRequired();
        b.Property(d => d.Description);
        b.Property(d => d.FileKey).IsRequired();
        b.Property(d => d.FileSizeBytes).IsRequired();
        b.Property(d => d.MimeType).HasMaxLength(100).IsRequired();
        b.Property(d => d.UploadedBy).IsRequired();

        b.Property(d => d.FolderType)
            .HasConversion(v => FolderToString(v), v => FolderFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        b.HasIndex(d => d.FolderType).HasDatabaseName("ix_documents_folder_type");

        b.HasQueryFilter(d => d.DeletedAt == null);
    }

    private static string FolderToString(DocumentFolderType v) => v switch
    {
        DocumentFolderType.Public => "public",
        DocumentFolderType.Private => "private",
        _ => throw new InvalidOperationException($"Unknown DocumentFolderType value: {v}"),
    };

    private static DocumentFolderType FolderFromString(string v) => v switch
    {
        "public" => DocumentFolderType.Public,
        "private" => DocumentFolderType.Private,
        _ => throw new InvalidOperationException($"Unknown document folder type string: '{v}'"),
    };
}

internal sealed class DocumentAssignmentConfiguration : IEntityTypeConfiguration<DocumentAssignment>
{
    public void Configure(EntityTypeBuilder<DocumentAssignment> b)
    {
        b.ToTable("document_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.DocumentId).IsRequired();
        b.Property(a => a.AssignedToRole).HasMaxLength(20);
        b.Property(a => a.AssignedToUserId);

        b.HasIndex(a => a.DocumentId).HasDatabaseName("ix_document_assignments_document_id");
        b.HasIndex(a => a.AssignedToUserId).HasDatabaseName("ix_document_assignments_user_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}
