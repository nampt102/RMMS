using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    private const string LatLngType = "numeric(10,7)";
    private const string DistanceType = "numeric(8,2)";
    private const string ConfidenceType = "numeric(5,4)";

    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("attendance_records");
        b.HasKey(a => a.Id);

        b.Property(a => a.UserId).IsRequired();
        b.Property(a => a.WorkScheduleShiftId).IsRequired();
        b.Property(a => a.StoreId).IsRequired();

        // ----- Check-in -----
        b.Property(a => a.CheckInAt).IsRequired();
        b.Property(a => a.CheckInLatitude).HasColumnType(LatLngType).IsRequired();
        b.Property(a => a.CheckInLongitude).HasColumnType(LatLngType).IsRequired();
        b.Property(a => a.CheckInDistanceMeters).HasColumnType(DistanceType).IsRequired();
        b.Property(a => a.CheckInFaceResult)
            .HasConversion(v => FaceToString(v), v => FaceFromString(v))
            .HasMaxLength(20).IsRequired();
        b.Property(a => a.CheckInFaceConfidence).HasColumnType(ConfidenceType);
        b.Property(a => a.CheckInSelfieUrl);
        b.Property(a => a.CheckInStorePhotoUrl);
        b.Property(a => a.CheckInFakeGpsDetected).IsRequired();
        b.Property(a => a.IsLate).IsRequired();
        b.Property(a => a.CheckInNote);

        // ----- Check-out (nullable) -----
        b.Property(a => a.CheckOutAt);
        b.Property(a => a.CheckOutLatitude).HasColumnType(LatLngType);
        b.Property(a => a.CheckOutLongitude).HasColumnType(LatLngType);
        b.Property(a => a.CheckOutDistanceMeters).HasColumnType(DistanceType);
        b.Property(a => a.CheckOutFaceResult)
            .HasConversion(
                v => v == null ? null : FaceToString(v.Value),
                v => v == null ? (FaceVerificationResult?)null : FaceFromString(v))
            .HasMaxLength(20);
        b.Property(a => a.CheckOutFaceConfidence).HasColumnType(ConfidenceType);
        b.Property(a => a.CheckOutSelfieUrl);
        b.Property(a => a.CheckOutStorePhotoUrl);
        b.Property(a => a.CheckOutNote);

        // ----- Status + review -----
        b.Property(a => a.Status)
            .HasConversion(v => StatusToString(v), v => StatusFromString(v))
            .HasMaxLength(30).IsRequired();
        b.Property(a => a.ReviewedBy);
        b.Property(a => a.ReviewedAt);
        b.Property(a => a.ReviewNote);

        // Index: history (user + most-recent first) and the review queue (status + created).
        b.HasIndex(a => new { a.UserId, a.CheckInAt }).HasDatabaseName("ix_attendance_records_user_check_in");
        b.HasIndex(a => new { a.Status, a.CreatedAt }).HasDatabaseName("ix_attendance_records_status_created");
        b.HasIndex(a => a.StoreId).HasDatabaseName("ix_attendance_records_store");
        b.HasIndex(a => a.WorkScheduleShiftId).HasDatabaseName("ix_attendance_records_shift");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }

    private static string FaceToString(FaceVerificationResult v) => v switch
    {
        FaceVerificationResult.Success => "success",
        FaceVerificationResult.Fail => "fail",
        FaceVerificationResult.PendingReview => "pending_review",
        _ => throw new InvalidOperationException($"Unknown FaceVerificationResult value: {v}"),
    };

    private static FaceVerificationResult FaceFromString(string v) => v switch
    {
        "success" => FaceVerificationResult.Success,
        "fail" => FaceVerificationResult.Fail,
        "pending_review" => FaceVerificationResult.PendingReview,
        _ => throw new InvalidOperationException($"Unknown face result string: '{v}'"),
    };

    private static string StatusToString(AttendanceStatus v) => v switch
    {
        AttendanceStatus.Valid => "valid",
        AttendanceStatus.Late => "late",
        AttendanceStatus.GpsViolationPendingReview => "gps_violation_pending_review",
        AttendanceStatus.FaceFailPendingReview => "face_fail_pending_review",
        AttendanceStatus.FakeGpsBlocked => "fake_gps_blocked",
        AttendanceStatus.AdminApproved => "admin_approved",
        AttendanceStatus.AdminRejected => "admin_rejected",
        _ => throw new InvalidOperationException($"Unknown AttendanceStatus value: {v}"),
    };

    private static AttendanceStatus StatusFromString(string v) => v switch
    {
        "valid" => AttendanceStatus.Valid,
        "late" => AttendanceStatus.Late,
        "gps_violation_pending_review" => AttendanceStatus.GpsViolationPendingReview,
        "face_fail_pending_review" => AttendanceStatus.FaceFailPendingReview,
        "fake_gps_blocked" => AttendanceStatus.FakeGpsBlocked,
        "admin_approved" => AttendanceStatus.AdminApproved,
        "admin_rejected" => AttendanceStatus.AdminRejected,
        _ => throw new InvalidOperationException($"Unknown attendance status string: '{v}'"),
    };
}
