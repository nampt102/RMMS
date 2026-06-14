using Rmms.Domain.Common;

namespace Rmms.Domain.Documents;

/// <summary>
/// One targeting rule granting access to a <see cref="Document"/> (M13,
/// <c>04-data-model.md</c> document_assignments). Multiple rows for one document = OR logic.
/// A null target field means "not scoped by that dimension"; a payslip is a single
/// <see cref="AssignedToUserId"/> assignment on a private document.
/// </summary>
public sealed class DocumentAssignment : AuditableEntity, IAggregateRoot
{
    public Guid DocumentId { get; private set; }

    /// <summary>"pg" / "leader" (null = not role-scoped).</summary>
    public string? AssignedToRole { get; private set; }
    public Guid? AssignedToUserId { get; private set; }

    private DocumentAssignment() { } // EF Core

    public static DocumentAssignment Create(Guid documentId, string? role, Guid? userId)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("Document id is required.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(role) && userId is null)
        {
            throw new ArgumentException("An assignment needs a role or a user.", nameof(userId));
        }

        return new DocumentAssignment
        {
            DocumentId = documentId,
            AssignedToRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToLowerInvariant(),
            AssignedToUserId = userId,
        };
    }
}
