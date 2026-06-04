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
