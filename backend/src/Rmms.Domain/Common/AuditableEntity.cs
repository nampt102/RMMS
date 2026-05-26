namespace Rmms.Domain.Common;

/// <summary>
/// Entity with audit timestamps and actor tracking.
/// Per <c>knowledge-base/08-coding-standards.md</c> — Database section: timestamps are <c>created_at</c>, <c>updated_at</c>, <c>deleted_at</c>.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}
