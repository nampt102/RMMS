using Rmms.Shared.Errors;

namespace Rmms.Api.Localization;

/// <summary>
/// Resolves a localized, user-facing message for an error <c>code</c> in the active culture.
/// </summary>
public interface IErrorMessageLocalizer
{
    /// <summary>Localized message for <paramref name="code"/>, or null if the code is not catalogued.</summary>
    string? Localize(string code, string culture);
}

/// <summary>
/// Code-keyed vi/en message catalog (Sprint 01 Day 8). We key by <see cref="ErrorCodes"/> rather
/// than authoring .resx XML: it is deterministic, unit-testable, and avoids resx path-convention
/// pitfalls — same outcome (Accept-Language → localized error message). Add new codes here as
/// modules ship; unknown codes fall back to the handler-provided default message.
/// </summary>
public sealed class ErrorMessageCatalog : IErrorMessageLocalizer
{
    private static readonly Dictionary<string, (string Vi, string En)> Messages =
        new(StringComparer.Ordinal)
        {
            [ErrorCodes.ValidationFailed] = ("Dữ liệu không hợp lệ.", "Invalid data."),
            [ErrorCodes.NotFound] = ("Không tìm thấy dữ liệu.", "Not found."),
            [ErrorCodes.Conflict] = ("Dữ liệu bị xung đột.", "Conflict."),
            [ErrorCodes.InternalError] = ("Đã xảy ra lỗi không mong muốn.", "Something went wrong."),
            [ErrorCodes.RateLimitExceeded] = ("Quá nhiều lần thử. Vui lòng đợi và thử lại sau.", "Too many attempts. Please wait and try again later."),
            [ErrorCodes.InvalidCredentials] = ("Email hoặc mật khẩu không đúng.", "Wrong email or password."),
            [ErrorCodes.EmailNotVerified] = ("Vui lòng xác minh email trước khi đăng nhập.", "Please verify your email before signing in."),
            [ErrorCodes.EmailAlreadyRegistered] = ("Email này đã được đăng ký.", "This email is already registered."),
            [ErrorCodes.AccountInactive] = ("Tài khoản đã bị vô hiệu hoá. Vui lòng liên hệ Admin.", "This account is inactive. Contact your administrator."),
            [ErrorCodes.AccountLocked] = ("Tài khoản tạm khoá do đăng nhập sai quá nhiều lần.", "Account temporarily locked after too many failed attempts."),
            [ErrorCodes.TokenExpired] = ("Phiên đăng nhập đã hết hạn.", "Your session has expired."),
            [ErrorCodes.TokenInvalid] = ("Bạn cần đăng nhập.", "Authentication required."),
            [ErrorCodes.RefreshTokenRevoked] = ("Token không hợp lệ hoặc đã bị thu hồi. Vui lòng đăng nhập lại.", "Token is invalid or revoked. Please sign in again."),
            [ErrorCodes.RefreshTokenReused] = ("Phát hiện token đã bị sử dụng lại. Tất cả phiên đã đăng xuất. Vui lòng đăng nhập lại.", "Token reuse detected. All sessions were signed out. Please sign in again."),
            [ErrorCodes.DeviceNotAuthorized] = ("Thiết bị này chưa được phê duyệt. Vui lòng liên hệ Leader hoặc Admin.", "This device is not authorized. Contact your Leader or Admin."),
            [ErrorCodes.PasswordTooWeak] = ("Mật khẩu quá yếu. Tối thiểu 8 ký tự, gồm 1 chữ và 1 số.", "Password is too weak. Use at least 8 characters with 1 letter and 1 digit."),
            [ErrorCodes.PermissionDenied] = ("Bạn không có quyền truy cập.", "You do not have permission to access this."),
            [ErrorCodes.EmailTokenExpired] = ("Liên kết xác minh đã hết hạn. Vui lòng đăng ký lại để nhận liên kết mới.", "The verification link has expired. Please register again for a new one."),
            [ErrorCodes.EmailTokenUsed] = ("Liên kết xác minh đã được sử dụng.", "This verification link has already been used."),
            [ErrorCodes.IdempotencyKeyReused] = ("Yêu cầu trùng lặp đang được xử lý.", "A duplicate request is already in progress."),
            [ErrorCodes.CodeAlreadyExists] = ("Mã này đã tồn tại. Vui lòng dùng mã khác.", "This code already exists. Please use a different one."),
            [ErrorCodes.InvalidReference] = ("Tham chiếu không hợp lệ hoặc không tồn tại.", "Referenced item is invalid or does not exist."),
            [ErrorCodes.InvalidAssignment] = ("Phân công không hợp lệ.", "Invalid assignment."),
            [ErrorCodes.AssignmentExists] = ("Phân công này đã tồn tại.", "This assignment already exists."),
            [ErrorCodes.NotApprover] = ("Bạn không có quyền duyệt yêu cầu của PG này.", "You are not authorized to approve this PG's request."),
            [ErrorCodes.RejectReasonRequired] = ("Vui lòng nhập lý do từ chối.", "A rejection reason is required."),
            [ErrorCodes.ApprovalNotPending] = ("Mục này không ở trạng thái chờ duyệt.", "This item is not pending approval."),
            [ErrorCodes.StoreNotAssigned] = ("Có điểm bán chưa được phân công cho người dùng.", "A store is not assigned to this user."),
            [ErrorCodes.ScheduleNotFound] = ("Không tìm thấy lịch làm việc.", "Work schedule not found."),
            [ErrorCodes.ScheduleNotEditable] = ("Lịch ở trạng thái này không thể sửa hoặc thu hồi.", "This schedule cannot be edited or withdrawn in its current state."),
            [ErrorCodes.ScheduleDateInPast] = ("Không thể đăng ký hoặc sửa lịch cho ngày đã qua.", "Cannot register or edit a schedule for a past date."),
            [ErrorCodes.ShiftOverlap] = ("Các ca trong cùng một ngày không được trùng giờ.", "Shifts on the same day must not overlap."),
            [ErrorCodes.ScheduleNotPending] = ("Chỉ lịch ở trạng thái chờ mới có thể gửi duyệt.", "Only a pending schedule can be submitted."),
            // ----- M05 Attendance -----
            [ErrorCodes.FakeGpsDetected] = ("Phát hiện GPS giả lập — không thể chấm công.", "Mock/fake GPS detected — check-in is blocked."),
            [ErrorCodes.AlreadyCheckedIn] = ("Bạn đang có ca chấm công chưa check-out.", "You have an open attendance that must be checked out first."),
            [ErrorCodes.NoOpenAttendance] = ("Lượt chấm công này không ở trạng thái có thể check-out.", "This attendance cannot be checked out."),
            [ErrorCodes.CheckInTooEarly] = ("Chưa đến giờ được phép check-in (sớm tối đa 60 phút).", "Too early to check in (up to 60 minutes before shift start)."),
            [ErrorCodes.ShiftNotFound] = ("Không có ca làm đã duyệt phù hợp để chấm công.", "No approved shift available to check in against."),
            [ErrorCodes.AttendanceNotFound] = ("Không tìm thấy lượt chấm công.", "Attendance record not found."),
            [ErrorCodes.AttendanceNotReviewable] = ("Lượt chấm công này không ở trạng thái chờ duyệt.", "This attendance is not pending review."),
            [ErrorCodes.FaceVerificationFailed] = ("Xác thực khuôn mặt thất bại.", "Face verification failed."),
            [ErrorCodes.OfflineNotSupported] = ("Không hỗ trợ chấm công offline.", "Offline check-in/out is not supported."),
        };

    public string? Localize(string code, string culture)
    {
        if (string.IsNullOrEmpty(code) || !Messages.TryGetValue(code, out var pair))
        {
            return null;
        }

        return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? pair.En : pair.Vi;
    }
}
