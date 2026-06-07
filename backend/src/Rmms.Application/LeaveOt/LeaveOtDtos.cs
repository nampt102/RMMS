using Rmms.Application.Common;
using Rmms.Domain.LeaveOt;

namespace Rmms.Application.LeaveOt;

/// <summary>Leave request projected for list/detail (M08).</summary>
public sealed record LeaveRequestDto(
    Guid Id,
    Guid UserId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string Reason,
    string Status,
    Guid? ApprovalId,
    Guid? LinkedAttendanceId,
    DateTimeOffset CreatedAt,
    string? RequesterName = null);

/// <summary>OT request projected for list/detail (M08).</summary>
public sealed record OtRequestDto(
    Guid Id,
    Guid UserId,
    DateOnly OtDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason,
    string Status,
    Guid? ApprovalId,
    DateTimeOffset CreatedAt,
    string? RequesterName = null);

internal static class LeaveOtMapper
{
    public static LeaveRequestDto ToDto(LeaveRequest r, string? requesterName = null) => new(
        r.Id, r.UserId, r.LeaveType.ToSnakeCase(), r.StartDate, r.EndDate, r.StartTime, r.EndTime,
        r.Reason, r.Status.ToSnakeCase(), r.ApprovalId, r.LinkedAttendanceId, r.CreatedAt, requesterName);

    public static OtRequestDto ToDto(OtRequest r, string? requesterName = null) => new(
        r.Id, r.UserId, r.OtDate, r.StartTime, r.EndTime, r.Reason, r.Status.ToSnakeCase(), r.ApprovalId, r.CreatedAt, requesterName);
}
