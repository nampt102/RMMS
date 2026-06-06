using System.Globalization;
using Rmms.Application.Common;
using Rmms.Domain.Scheduling;

namespace Rmms.Application.Scheduling;

// ===== Request inputs (shared by create/edit) =====

/// <summary>One shift in a create/edit request: a store + local time window.</summary>
public sealed record ScheduleShiftRequest(Guid StoreId, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>One day in a create request (BR-301: client expands week/month into days).</summary>
public sealed record ScheduleDayRequest(DateOnly Date, IReadOnlyList<ScheduleShiftRequest> Shifts);

// ===== Output DTOs =====

public sealed record WorkScheduleShiftDto(
    Guid Id,
    Guid StoreId,
    string StoreCode,
    string StoreName,
    string StartTime,
    string EndTime,
    int Ordering);

public sealed record WorkScheduleDto(
    Guid Id,
    Guid UserId,
    DateOnly ScheduleDate,
    string Status,
    int Version,
    Guid? PreviousVersionId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    string? RejectReason,
    IReadOnlyList<WorkScheduleShiftDto> Shifts);

/// <summary>Maps domain <see cref="WorkSchedule"/> to its API DTO, resolving store labels from a lookup.</summary>
public static class WorkScheduleMapper
{
    public static WorkScheduleDto ToDto(
        Domain.Scheduling.WorkSchedule schedule,
        IReadOnlyDictionary<Guid, (string Code, string Name)> stores)
    {
        var shifts = schedule.Shifts
            .OrderBy(s => s.Ordering)
            .Select(s =>
            {
                var label = stores.TryGetValue(s.StoreId, out var v) ? v : (Code: string.Empty, Name: string.Empty);
                return new WorkScheduleShiftDto(
                    s.Id,
                    s.StoreId,
                    label.Code,
                    label.Name,
                    s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                    s.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                    s.Ordering);
            })
            .ToList();

        return new WorkScheduleDto(
            schedule.Id,
            schedule.UserId,
            schedule.ScheduleDate,
            schedule.Status.ToSnakeCase(),
            schedule.Version,
            schedule.PreviousVersionId,
            schedule.SubmittedAt,
            schedule.ApprovedAt,
            schedule.RejectReason,
            shifts);
    }
}
