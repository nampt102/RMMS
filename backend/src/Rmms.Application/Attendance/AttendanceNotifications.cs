using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;

namespace Rmms.Application.Attendance;

/// <summary>Notification specs for attendance events (M14 / CR-2).</summary>
internal static class AttendanceNotifications
{
    /// <summary>
    /// "Your attendance was sent to Admin Review" (CR-2). CR-3: in-app only — no push/email,
    /// to avoid alarming the PG before a human has looked at it.
    /// </summary>
    public static NotificationSpec InReview(AttendanceRecord record)
    {
        var (kindVi, kindEn) = record.Status == AttendanceStatus.FaceFailPendingReview
            ? ("xác thực khuôn mặt", "face verification")
            : ("vị trí GPS", "GPS location");

        return new NotificationSpec(
            NotificationType.AttendanceInReview,
            TitleVi: "Chấm công đang chờ duyệt",
            TitleEn: "Attendance under review",
            BodyVi: $"Lượt chấm công của bạn cần Admin xem lại ({kindVi}). Bạn sẽ được thông báo khi có kết quả.",
            BodyEn: $"Your attendance needs admin review ({kindEn}). You'll be notified of the decision.",
            Data: new Dictionary<string, string>
            {
                ["deepLink"] = $"rmms://attendance/{record.Id}",
                ["entityType"] = "attendance",
                ["entityId"] = record.Id.ToString(),
            },
            Push: false,
            Email: false);
    }
}
