namespace Rmms.Api.Dtos.Scheduling;

/// <summary>One shift in a request. Times are "HH:mm" strings (e.g. "08:00"); the controller parses them.</summary>
public sealed record ShiftRequestDto(Guid StoreId, string StartTime, string EndTime);

/// <summary>One day's shifts when registering a schedule.</summary>
public sealed record ScheduleDayRequestDto(DateOnly Date, List<ShiftRequestDto> Shifts);

/// <summary>POST /schedule/me — register one or more days (client expands week/month into days).</summary>
public sealed record CreateScheduleRequest(List<ScheduleDayRequestDto> Days);

/// <summary>PATCH /schedule/{id} — replace the shifts of a schedule.</summary>
public sealed record EditScheduleRequest(List<ShiftRequestDto> Shifts);

/// <summary>POST /schedule/{id}/reject — reason is required (BR-404).</summary>
public sealed record RejectScheduleRequest(string Reason);
