using Rmms.Domain.Common;

namespace Rmms.Domain.Forms;

/// <summary>
/// One targeting rule that makes a <see cref="Form"/> available to fillers (M10 +
/// <c>04-data-model.md</c> form_assignments). Multiple rows for one form = OR logic. Targets the
/// form (not a version) so the latest published version is always served. A null target field
/// means "not scoped by that dimension".
/// </summary>
public sealed class FormAssignment : AuditableEntity, IAggregateRoot
{
    public Guid FormId { get; private set; }

    /// <summary>"pg" / "leader" (null = not role-scoped).</summary>
    public string? AssignedToRole { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? AssignedToStoreId { get; private set; }
    public Guid? AssignedToAreaId { get; private set; }
    public Guid? AssignedToCategoryId { get; private set; }
    public Guid? AssignedToProductId { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    private FormAssignment() { } // EF Core

    public static FormAssignment Create(
        Guid formId, string? role, Guid? userId, Guid? storeId, Guid? areaId, Guid? categoryId, Guid? productId,
        DateTimeOffset validFrom, DateTimeOffset? validTo)
    {
        if (formId == Guid.Empty) throw new ArgumentException("Form id is required.", nameof(formId));

        return new FormAssignment
        {
            FormId = formId,
            AssignedToRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToLowerInvariant(),
            AssignedToUserId = userId,
            AssignedToStoreId = storeId,
            AssignedToAreaId = areaId,
            AssignedToCategoryId = categoryId,
            AssignedToProductId = productId,
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }
}
