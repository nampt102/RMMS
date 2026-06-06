namespace Rmms.Domain.Enums;

/// <summary>
/// Lifecycle of a work schedule (one PG/Leader, one day) per <c>04-data-model.md</c>
/// (work_schedules.status) and <c>06-business-rules.md</c> BR-301..BR-308 — M07.
/// Stored as snake_case string in DB.
///
/// Versioning (BR-307/BR-308): editing an <see cref="Approved"/> schedule creates a NEW
/// row in <see cref="EditPending"/> (with <c>previous_version_id</c>) while the old row
/// stays <see cref="Approved"/> and effective; on approval of the edit the old row becomes
/// <see cref="Superseded"/>.
/// </summary>
public enum WorkScheduleStatus
{
    /// <summary>Draft or submitted-and-awaiting first approval (submitted_at distinguishes).</summary>
    Pending = 1,

    /// <summary>Approved and effective.</summary>
    Approved = 2,

    /// <summary>Rejected by approver (reject_reason set).</summary>
    Rejected = 3,

    /// <summary>An edit to an already-approved schedule, awaiting approval; old row stays effective.</summary>
    EditPending = 4,

    /// <summary>A previously-approved version replaced by an approved edit.</summary>
    Superseded = 5,
}
