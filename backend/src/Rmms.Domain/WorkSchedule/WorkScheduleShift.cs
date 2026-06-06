using Rmms.Domain.Common;

namespace Rmms.Domain.Scheduling;

/// <summary>
/// A single shift within a <see cref="WorkSchedule"/> (M07, <c>04-data-model.md</c>
/// work_schedule_shifts). One shift = one store (BR-305); a day may hold multiple
/// shifts across multiple stores (BR-304/BR-305) as long as their times don't overlap.
///
/// Child of the <see cref="WorkSchedule"/> aggregate — never created or queried on its own.
/// </summary>
public sealed class WorkScheduleShift : Entity
{
    public Guid WorkScheduleId { get; private set; }
    public Guid StoreId { get; private set; }

    /// <summary>Local shift start, e.g. 08:00 (Phase 1 assumes VN time — see M05 notes).</summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>Local shift end, e.g. 17:00. Must be strictly after <see cref="StartTime"/>.</summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>0-based position when a day has multiple shifts (sorted by start time).</summary>
    public int Ordering { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private WorkScheduleShift() { }

    internal WorkScheduleShift(Guid workScheduleId, Guid storeId, TimeOnly startTime, TimeOnly endTime, int ordering)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(storeId));
        if (endTime <= startTime) throw new InvalidOperationException("Shift end_time must be after start_time.");

        WorkScheduleId = workScheduleId;
        StoreId = storeId;
        StartTime = startTime;
        EndTime = endTime;
        Ordering = ordering;
    }

    /// <summary>Mutate this shift in place (used when reconciling a schedule edit).</summary>
    internal void Update(Guid storeId, TimeOnly startTime, TimeOnly endTime, int ordering)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(storeId));
        if (endTime <= startTime) throw new InvalidOperationException("Shift end_time must be after start_time.");

        StoreId = storeId;
        StartTime = startTime;
        EndTime = endTime;
        Ordering = ordering;
    }

    /// <summary>True when this shift's time window overlaps the other's (same day).</summary>
    public bool OverlapsWith(WorkScheduleShift other) =>
        StartTime < other.EndTime && other.StartTime < EndTime;
}
