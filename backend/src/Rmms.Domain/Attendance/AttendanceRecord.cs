using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Attendance;

/// <summary>Validated facts for a check-in, fed to <see cref="AttendanceRecord.CheckIn"/>.</summary>
public sealed record AttendanceCheckInData(
    Guid UserId,
    Guid WorkScheduleShiftId,
    Guid StoreId,
    DateTimeOffset CheckInAt,
    decimal Latitude,
    decimal Longitude,
    decimal DistanceMeters,
    bool FakeGpsDetected,
    FaceVerificationResult FaceResult,
    decimal? FaceConfidence,
    string? SelfieUrl,
    string? StorePhotoUrl,
    bool IsLate,
    string? Note);

/// <summary>Validated facts for a check-out, fed to <see cref="AttendanceRecord.CheckOut"/>.</summary>
public sealed record AttendanceCheckOutData(
    DateTimeOffset CheckOutAt,
    decimal Latitude,
    decimal Longitude,
    decimal DistanceMeters,
    FaceVerificationResult FaceResult,
    decimal? FaceConfidence,
    string? SelfieUrl,
    string? StorePhotoUrl,
    string? Note);

/// <summary>
/// One check-in (+ optional check-out) for a PG/Leader against a scheduled shift (M05,
/// <c>04-data-model.md</c> attendance_records). Anti-fraud per BR-201..BR-210: GPS geofence
/// (≤<see cref="GeofenceRadiusMeters"/> m — BR-204), fake-GPS block (BR-205), mandatory Face
/// Verification at both ends (BR-206), and the status decision matrix (§3.2).
///
/// The <see cref="Status"/> state machine is owned here; the application layer computes the
/// raw facts (distance, lateness, face result) and the domain decides the resulting status.
/// </summary>
public sealed class AttendanceRecord : AuditableEntity, IAggregateRoot
{
    /// <summary>BR-204 geofence radius: a check-in &gt; this distance from the store goes to Admin review.</summary>
    public const int GeofenceRadiusMeters = 300;

    public Guid UserId { get; private set; }
    public Guid WorkScheduleShiftId { get; private set; }
    public Guid StoreId { get; private set; }

    // ----- Check-in -----
    public DateTimeOffset CheckInAt { get; private set; }
    public decimal CheckInLatitude { get; private set; }
    public decimal CheckInLongitude { get; private set; }
    public decimal CheckInDistanceMeters { get; private set; }
    public FaceVerificationResult CheckInFaceResult { get; private set; }
    public decimal? CheckInFaceConfidence { get; private set; }
    public string? CheckInSelfieUrl { get; private set; }
    public string? CheckInStorePhotoUrl { get; private set; }
    public bool CheckInFakeGpsDetected { get; private set; }
    public bool IsLate { get; private set; }
    public string? CheckInNote { get; private set; }

    // ----- Check-out (nullable until checked out) -----
    public DateTimeOffset? CheckOutAt { get; private set; }
    public decimal? CheckOutLatitude { get; private set; }
    public decimal? CheckOutLongitude { get; private set; }
    public decimal? CheckOutDistanceMeters { get; private set; }
    public FaceVerificationResult? CheckOutFaceResult { get; private set; }
    public decimal? CheckOutFaceConfidence { get; private set; }
    public string? CheckOutSelfieUrl { get; private set; }
    public string? CheckOutStorePhotoUrl { get; private set; }
    public string? CheckOutNote { get; private set; }

    // ----- Status + review -----
    public AttendanceStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }

    private AttendanceRecord() { }

    /// <summary>
    /// Create a check-in record, deriving <see cref="Status"/> from the validated facts per the
    /// §3.2 decision matrix. GPS violation takes precedence over a face failure; a fake-GPS
    /// check-in is recorded as <see cref="AttendanceStatus.FakeGpsBlocked"/> (audit only, BR-205).
    /// </summary>
    public static AttendanceRecord CheckIn(AttendanceCheckInData data)
    {
        if (data.UserId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(data));
        if (data.WorkScheduleShiftId == Guid.Empty) throw new ArgumentException("Shift id is required.", nameof(data));
        if (data.StoreId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(data));

        return new AttendanceRecord
        {
            UserId = data.UserId,
            WorkScheduleShiftId = data.WorkScheduleShiftId,
            StoreId = data.StoreId,
            CheckInAt = data.CheckInAt,
            CheckInLatitude = data.Latitude,
            CheckInLongitude = data.Longitude,
            CheckInDistanceMeters = data.DistanceMeters,
            CheckInFaceResult = data.FaceResult,
            CheckInFaceConfidence = data.FaceConfidence,
            CheckInSelfieUrl = data.SelfieUrl,
            CheckInStorePhotoUrl = data.StorePhotoUrl,
            CheckInFakeGpsDetected = data.FakeGpsDetected,
            IsLate = data.IsLate,
            CheckInNote = string.IsNullOrWhiteSpace(data.Note) ? null : data.Note.Trim(),
            Status = DetermineCheckInStatus(data),
        };
    }

    private static AttendanceStatus DetermineCheckInStatus(AttendanceCheckInData data)
    {
        if (data.FakeGpsDetected) return AttendanceStatus.FakeGpsBlocked;          // BR-205
        if (data.DistanceMeters > GeofenceRadiusMeters) return AttendanceStatus.GpsViolationPendingReview; // BR-204
        if (data.FaceResult != FaceVerificationResult.Success) return AttendanceStatus.FaceFailPendingReview; // BR-207
        return data.IsLate ? AttendanceStatus.Late : AttendanceStatus.Valid;        // BR-203
    }

    /// <summary>True while this record is checked-in and awaiting a check-out (blocks a new check-in).</summary>
    public bool IsOpen =>
        CheckOutAt is null &&
        Status is AttendanceStatus.Valid
            or AttendanceStatus.Late
            or AttendanceStatus.GpsViolationPendingReview
            or AttendanceStatus.FaceFailPendingReview
            or AttendanceStatus.AdminApproved;

    /// <summary>True when the record sits in the Admin review queue (AC-9).</summary>
    public bool RequiresReview =>
        Status is AttendanceStatus.GpsViolationPendingReview or AttendanceStatus.FaceFailPendingReview;

    /// <summary>
    /// Close this attendance with a check-out. Face is mandatory at check-out too (BR-206); a
    /// check-out GPS violation or face failure escalates an otherwise-valid record to the review
    /// queue. Fake-GPS at check-out is blocked by the application layer before reaching here.
    /// </summary>
    public void CheckOut(AttendanceCheckOutData data)
    {
        if (CheckOutAt is not null) throw new InvalidOperationException("Attendance is already checked out.");
        if (!IsOpen) throw new InvalidOperationException("Only an open attendance can be checked out.");

        CheckOutAt = data.CheckOutAt;
        CheckOutLatitude = data.Latitude;
        CheckOutLongitude = data.Longitude;
        CheckOutDistanceMeters = data.DistanceMeters;
        CheckOutFaceResult = data.FaceResult;
        CheckOutFaceConfidence = data.FaceConfidence;
        CheckOutSelfieUrl = data.SelfieUrl;
        CheckOutStorePhotoUrl = data.StorePhotoUrl;
        CheckOutNote = string.IsNullOrWhiteSpace(data.Note) ? null : data.Note.Trim();

        // Escalate a clean record if the check-out itself is anomalous (GPS wins over face).
        if (Status is AttendanceStatus.Valid or AttendanceStatus.Late)
        {
            if (data.DistanceMeters > GeofenceRadiusMeters)
            {
                Status = AttendanceStatus.GpsViolationPendingReview;
            }
            else if (data.FaceResult != FaceVerificationResult.Success)
            {
                Status = AttendanceStatus.FaceFailPendingReview;
            }
        }
    }

    /// <summary>Admin confirms the record is valid after review (BR-208) → <see cref="AttendanceStatus.AdminApproved"/>.</summary>
    public void ApproveReview(Guid reviewerId, string? note, DateTimeOffset now)
    {
        if (!RequiresReview) throw new InvalidOperationException("Only a pending-review attendance can be reviewed.");
        Status = AttendanceStatus.AdminApproved;
        StampReview(reviewerId, note, now);
    }

    /// <summary>Admin rejects the record after review (BR-209) → <see cref="AttendanceStatus.AdminRejected"/>. Reason required (BR-404).</summary>
    public void RejectReview(Guid reviewerId, string note, DateTimeOffset now)
    {
        if (!RequiresReview) throw new InvalidOperationException("Only a pending-review attendance can be reviewed.");
        if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Review reason is required.", nameof(note));
        Status = AttendanceStatus.AdminRejected;
        StampReview(reviewerId, note, now);
    }

    /// <summary>
    /// Clear photo references after the object-storage copies are deleted (CR-4 — selfies/store
    /// photos retained 90 days). Keeps the attendance/audit record itself for compliance.
    /// </summary>
    public void PurgePhotos()
    {
        CheckInSelfieUrl = null;
        CheckInStorePhotoUrl = null;
        CheckOutSelfieUrl = null;
        CheckOutStorePhotoUrl = null;
    }

    private void StampReview(Guid reviewerId, string? note, DateTimeOffset now)
    {
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
