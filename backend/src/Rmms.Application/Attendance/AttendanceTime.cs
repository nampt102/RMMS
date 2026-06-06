using Rmms.Domain.Enums;

namespace Rmms.Application.Attendance;

/// <summary>
/// Shared attendance time/threshold helpers. Phase 1 assumes all stores are in Vietnam
/// (CR-5): shift <see cref="TimeOnly"/> windows are interpreted in <c>Asia/Ho_Chi_Minh</c>
/// (UTC+7) and compared against the UTC clock. Multi-timezone handling is post-Phase-1.
/// </summary>
internal static class AttendanceTime
{
    public static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    /// <summary>BR-202: a check-in is allowed up to this many minutes before shift start.</summary>
    public const int EarlyCheckInMinutes = 60;

    /// <summary>BR-203: a check-in more than this many minutes after shift start is marked Late.</summary>
    public const int LateThresholdMinutes = 5;

    /// <summary>The VN-local calendar date for a UTC instant (used to find the day's schedule).</summary>
    public static DateOnly VnToday(DateTimeOffset utcNow) =>
        DateOnly.FromDateTime(utcNow.ToOffset(VnOffset).DateTime);

    /// <summary>Convert a VN-local (date, time) shift boundary to its UTC instant.</summary>
    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
        new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, VnOffset)
            .ToUniversalTime();

    /// <summary>Statuses for which a record still counts as a live check-in (no check-out yet).</summary>
    public static readonly AttendanceStatus[] OpenStatuses =
    {
        AttendanceStatus.Valid,
        AttendanceStatus.Late,
        AttendanceStatus.GpsViolationPendingReview,
        AttendanceStatus.FaceFailPendingReview,
        AttendanceStatus.AdminApproved,
    };
}
