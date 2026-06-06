namespace Rmms.Domain.Enums;

/// <summary>
/// Outcome of the Face Verification biometric step at check-in/check-out (BR-206, M06).
/// Stored snake_case in <c>attendance_records.check_in_face_result</c> /
/// <c>check_out_face_result</c> (varchar): <c>success</c> / <c>fail</c> / <c>pending_review</c>.
///
/// Phase 1A note: M06 (FPT.AI Face) is not wired yet — the stub face service returns
/// <see cref="Success"/> so the attendance flow is fully exercisable; swap the
/// implementation when M06 lands (the state machine already handles <see cref="Fail"/>).
/// </summary>
public enum FaceVerificationResult
{
    Success = 1,
    Fail = 2,
    PendingReview = 3,
}
