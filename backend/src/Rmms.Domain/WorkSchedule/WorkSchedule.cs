using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Scheduling;

/// <summary>Input for one shift when creating/editing a schedule (store + local time window).</summary>
public sealed record ScheduleShiftInput(Guid StoreId, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>
/// A PG/Leader's work schedule for ONE day (M07, <c>04-data-model.md</c> work_schedules).
/// Holds 1..N <see cref="WorkScheduleShift"/> (BR-304 multi-shift, BR-305 multi-store) whose
/// time windows must not overlap.
///
/// Approval + versioning (BR-307/BR-308):
///  - Create → <see cref="WorkScheduleStatus.Pending"/> (draft). <see cref="Submit"/> stamps submitted_at.
///  - Editing a still-<see cref="WorkScheduleStatus.Pending"/> row edits it in place (<see cref="ReplaceShifts"/>).
///  - Editing an <see cref="WorkScheduleStatus.Approved"/> row creates a NEW
///    <see cref="WorkScheduleStatus.EditPending"/> row (<see cref="CreateEditedVersion"/>) while the old
///    row stays approved + effective; on approval the old row is <see cref="Supersede"/>d.
/// </summary>
public sealed class WorkSchedule : AuditableEntity, IAggregateRoot
{
    private readonly List<WorkScheduleShift> _shifts = new();

    public Guid UserId { get; private set; }
    public DateOnly ScheduleDate { get; private set; }
    public WorkScheduleStatus Status { get; private set; }
    public int Version { get; private set; }
    public Guid? PreviousVersionId { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? RejectReason { get; private set; }

    public IReadOnlyList<WorkScheduleShift> Shifts => _shifts;

    private WorkSchedule() { }

    /// <summary>Create a new draft schedule (status = Pending, version 1).</summary>
    public static WorkSchedule Create(Guid userId, DateOnly scheduleDate, IReadOnlyList<ScheduleShiftInput> shifts)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));

        var schedule = new WorkSchedule
        {
            UserId = userId,
            ScheduleDate = scheduleDate,
            Status = WorkScheduleStatus.Pending,
            Version = 1,
        };
        schedule.SetShifts(shifts);
        return schedule;
    }

    /// <summary>Mark this draft as submitted for approval (BR-307).</summary>
    public void Submit(DateTimeOffset now)
    {
        if (Status != WorkScheduleStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending schedule can be submitted.");
        }
        SubmittedAt = now;
    }

    /// <summary>
    /// Replace the shifts of a not-yet-approved (pending/edit-pending) schedule. Reconciles in
    /// place — mutating existing shift rows and adding/trimming as needed — rather than
    /// delete-and-recreate, so an unchanged-count edit issues UPDATEs (cleaner churn).
    /// </summary>
    public void ReplaceShifts(IReadOnlyList<ScheduleShiftInput> shifts)
    {
        if (Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            throw new InvalidOperationException("Only a pending/edit-pending schedule can be edited in place.");
        }

        var ordered = ValidateAndOrder(shifts);
        for (var i = 0; i < ordered.Count; i++)
        {
            if (i < _shifts.Count)
            {
                _shifts[i].Update(ordered[i].StoreId, ordered[i].StartTime, ordered[i].EndTime, i);
            }
            else
            {
                _shifts.Add(new WorkScheduleShift(Id, ordered[i].StoreId, ordered[i].StartTime, ordered[i].EndTime, i));
            }
        }
        while (_shifts.Count > ordered.Count)
        {
            _shifts.RemoveAt(_shifts.Count - 1);
        }
    }

    /// <summary>
    /// Produce a new EDIT version of this (approved) schedule (BR-308). The returned row is
    /// <see cref="WorkScheduleStatus.EditPending"/> with <c>previous_version_id</c> pointing here
    /// and submitted_at stamped; THIS row stays approved + effective until the edit is approved.
    /// </summary>
    public WorkSchedule CreateEditedVersion(IReadOnlyList<ScheduleShiftInput> shifts, DateTimeOffset now)
    {
        if (Status != WorkScheduleStatus.Approved)
        {
            throw new InvalidOperationException("Only an approved schedule produces an edit version.");
        }

        var edit = new WorkSchedule
        {
            UserId = UserId,
            ScheduleDate = ScheduleDate,
            Status = WorkScheduleStatus.EditPending,
            Version = Version + 1,
            PreviousVersionId = Id,
            SubmittedAt = now,
        };
        edit.SetShifts(shifts);
        return edit;
    }

    /// <summary>Approve a pending / edit-pending schedule (BR-405/BR-406 routing handled in the app layer).</summary>
    public void Approve(Guid approverId, DateTimeOffset now)
    {
        if (Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            throw new InvalidOperationException("Only a pending/edit-pending schedule can be approved.");
        }
        Status = WorkScheduleStatus.Approved;
        ApprovedBy = approverId;
        ApprovedAt = now;
        RejectReason = null;
    }

    /// <summary>Reject a pending / edit-pending schedule. Reason is required (BR-404).</summary>
    public void Reject(Guid approverId, string reason, DateTimeOffset now)
    {
        if (Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            throw new InvalidOperationException("Only a pending/edit-pending schedule can be rejected.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reject reason is required.", nameof(reason));
        }
        Status = WorkScheduleStatus.Rejected;
        ApprovedBy = approverId;
        ApprovedAt = now;
        RejectReason = reason.Trim();
    }

    /// <summary>Mark this approved version as replaced by a newly-approved edit (BR-308).</summary>
    public void Supersede()
    {
        if (Status != WorkScheduleStatus.Approved)
        {
            throw new InvalidOperationException("Only an approved schedule can be superseded.");
        }
        Status = WorkScheduleStatus.Superseded;
    }

    private void SetShifts(IReadOnlyList<ScheduleShiftInput> inputs)
    {
        var ordered = ValidateAndOrder(inputs);
        _shifts.Clear();
        for (var i = 0; i < ordered.Count; i++)
        {
            _shifts.Add(new WorkScheduleShift(Id, ordered[i].StoreId, ordered[i].StartTime, ordered[i].EndTime, i));
        }
    }

    /// <summary>Validate (non-empty, end&gt;start, no overlap — BR-305) and return shifts sorted by start.</summary>
    private static List<ScheduleShiftInput> ValidateAndOrder(IReadOnlyList<ScheduleShiftInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
        {
            throw new InvalidOperationException("A schedule must have at least one shift.");
        }
        foreach (var s in inputs)
        {
            if (s.EndTime <= s.StartTime)
            {
                throw new InvalidOperationException("Shift end_time must be after start_time.");
            }
        }

        var ordered = inputs.OrderBy(s => s.StartTime).ThenBy(s => s.EndTime).ToList();
        var maxEnd = ordered[0].EndTime;
        for (var i = 1; i < ordered.Count; i++)
        {
            // A person cannot be in two places at once: reject any time-window overlap.
            if (ordered[i].StartTime < maxEnd)
            {
                throw new InvalidOperationException("Shift time windows must not overlap within a day.");
            }
            if (ordered[i].EndTime > maxEnd) maxEnd = ordered[i].EndTime;
        }
        return ordered;
    }
}
