namespace Rmms.Domain.Enums;

/// <summary>
/// Lifecycle/validity status of an <c>attendance_records</c> row (M05, status decision matrix
/// in <c>06-business-rules.md</c> §3.2). Stored snake_case in <c>attendance_records.status</c>.
///
/// Terminal-on-create: <see cref="FakeGpsBlocked"/> (BR-205 — recorded for audit but never a
/// valid attendance). Review outcomes: <see cref="AdminApproved"/> / <see cref="AdminRejected"/>
/// (BR-208/BR-209). The two <c>*PendingReview</c> values are the Admin review queue (AC-9).
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Within geofence, real GPS, face passed, on time.</summary>
    Valid = 1,

    /// <summary>As <see cref="Valid"/> but checked in &gt;5 min after shift start (BR-203).</summary>
    Late = 2,

    /// <summary>GPS distance &gt;300 m from store → Admin review (BR-204 / AC-9).</summary>
    GpsViolationPendingReview = 3,

    /// <summary>Face verification failed / inconclusive → Admin review (BR-207).</summary>
    FaceFailPendingReview = 4,

    /// <summary>Mock-location / rooted-device GPS → blocked, not a valid record (BR-205 / AC-10).</summary>
    FakeGpsBlocked = 5,

    /// <summary>Admin confirmed it IS the right person/place after review (BR-208).</summary>
    AdminApproved = 6,

    /// <summary>Admin confirmed it is NOT valid after review → not counted (BR-209).</summary>
    AdminRejected = 7,
}
