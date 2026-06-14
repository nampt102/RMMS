using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Documents;

/// <summary>
/// A stored document (M13, <c>04-data-model.md</c> documents): public material or a private file
/// such as a payslip. The binary lives in object storage under a random <see cref="FileKey"/>
/// (never a predictable name); access is granted via <see cref="DocumentAssignment"/> rows
/// (role / user, OR logic) and downloads mint a short-lived signed URL.
/// </summary>
public sealed class Document : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DocumentFolderType FolderType { get; private set; }

    /// <summary>Object-storage key (random) — NOT a public URL.</summary>
    public string FileKey { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public Guid UploadedBy { get; private set; }

    private Document() { } // EF Core

    public static Document Create(
        string name, string? description, DocumentFolderType folderType,
        string fileKey, long fileSizeBytes, string mimeType, Guid uploadedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        if (uploadedBy == Guid.Empty) throw new ArgumentException("Uploader id is required.", nameof(uploadedBy));

        return new Document
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            FolderType = folderType,
            FileKey = fileKey,
            FileSizeBytes = fileSizeBytes < 0 ? 0 : fileSizeBytes,
            MimeType = mimeType,
            UploadedBy = uploadedBy,
        };
    }

    public bool IsPrivate => FolderType == DocumentFolderType.Private;
}
