namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Emit an audit log row per <c>06-business-rules.md</c> CR-1.
/// The implementation persists to <c>audit_log</c> table.
///
/// NOTE: this interface does NOT call <c>SaveChangesAsync</c> — caller's UoW controls when the row is flushed,
/// so audit + business changes commit (or roll back) atomically.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Record an audit entry. Caller must <c>SaveChangesAsync</c>.</summary>
    Task RecordAsync(
        string action,
        string targetEntity,
        Guid? targetId,
        object? metadata = null,
        CancellationToken ct = default);
}
