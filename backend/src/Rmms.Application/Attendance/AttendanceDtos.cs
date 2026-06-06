using Rmms.Application.Common;
using Rmms.Domain.Attendance;

namespace Rmms.Application.Attendance;

/// <summary>Full attendance record for history / detail / admin views.</summary>
public sealed record AttendanceDto(
    Guid Id,
    Guid UserId,
    Guid WorkScheduleShiftId,
    Guid StoreId,
    string StoreCode,
    string StoreName,
    string Status,
    bool IsLate,
    // check-in
    DateTimeOffset CheckInAt,
    decimal CheckInLatitude,
    decimal CheckInLongitude,
    decimal CheckInDistanceMeters,
    string CheckInFaceResult,
    decimal? CheckInFaceConfidence,
    string? CheckInSelfieUrl,
    string? CheckInStorePhotoUrl,
    bool CheckInFakeGpsDetected,
    string? CheckInNote,
    // check-out (nullable)
    DateTimeOffset? CheckOutAt,
    decimal? CheckOutLatitude,
    decimal? CheckOutLongitude,
    decimal? CheckOutDistanceMeters,
    string? CheckOutFaceResult,
    decimal? CheckOutFaceConfidence,
    string? CheckOutSelfieUrl,
    string? CheckOutStorePhotoUrl,
    string? CheckOutNote,
    // review
    Guid? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote);

/// <summary>One shift the caller is expected to work today, plus its current attendance (if any).</summary>
public sealed record TodayShiftDto(
    Guid WorkScheduleShiftId,
    Guid StoreId,
    string StoreCode,
    string StoreName,
    decimal StoreLatitude,
    decimal StoreLongitude,
    string StartTime,
    string EndTime,
    Guid? AttendanceId,
    string? AttendanceStatus,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt);

/// <summary>A store the caller is assigned to (for the check-in store picker / nearest detection).</summary>
public sealed record AssignedStoreDto(
    Guid StoreId,
    string Code,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude);

/// <summary>Check-in screen bootstrap: assigned stores + today's shifts + the validation thresholds.</summary>
public sealed record CheckInInfoDto(
    int GeofenceRadiusMeters,
    int EarlyCheckInMinutes,
    int LateThresholdMinutes,
    IReadOnlyList<AssignedStoreDto> Stores,
    IReadOnlyList<TodayShiftDto> TodayShifts);

/// <summary>Maps the <see cref="AttendanceRecord"/> aggregate to its API DTO.</summary>
public static class AttendanceMapper
{
    public static AttendanceDto ToDto(AttendanceRecord a, string storeCode, string storeName) =>
        new(
            a.Id,
            a.UserId,
            a.WorkScheduleShiftId,
            a.StoreId,
            storeCode,
            storeName,
            a.Status.ToSnakeCase(),
            a.IsLate,
            a.CheckInAt,
            a.CheckInLatitude,
            a.CheckInLongitude,
            a.CheckInDistanceMeters,
            a.CheckInFaceResult.ToSnakeCase(),
            a.CheckInFaceConfidence,
            a.CheckInSelfieUrl,
            a.CheckInStorePhotoUrl,
            a.CheckInFakeGpsDetected,
            a.CheckInNote,
            a.CheckOutAt,
            a.CheckOutLatitude,
            a.CheckOutLongitude,
            a.CheckOutDistanceMeters,
            a.CheckOutFaceResult?.ToSnakeCase(),
            a.CheckOutFaceConfidence,
            a.CheckOutSelfieUrl,
            a.CheckOutStorePhotoUrl,
            a.CheckOutNote,
            a.ReviewedBy,
            a.ReviewedAt,
            a.ReviewNote);
}
