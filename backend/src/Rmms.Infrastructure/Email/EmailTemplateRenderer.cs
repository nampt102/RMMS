using System.Globalization;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Email;

namespace Rmms.Infrastructure.Email;

/// <summary>
/// String-interpolation based email template renderer for Sprint 01.
///
/// HTML templating engine (RazorLight / Scriban) is deferred to Sprint 03
/// when M14 News needs richer HTML newsletters.
/// </summary>
internal sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly AppUrlOptions _urls;
    private readonly string _brandName;

    public EmailTemplateRenderer(IOptions<AppUrlOptions> urls, IOptions<EmailOptions> emailOptions)
    {
        _urls = urls.Value;
        _brandName = string.IsNullOrWhiteSpace(emailOptions.Value.BrandName) ? "RMMS" : emailOptions.Value.BrandName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Verify Email
    // ─────────────────────────────────────────────────────────────────────────

    public EmailMessage BuildVerifyEmail(string toEmail, string toName, string tokenPlaintext, string language)
    {
        var lang = Normalize(language);
        var url = BuildVerifyEmailUrl(tokenPlaintext);

        var subject = lang == "en"
            ? $"Verify your {_brandName} account"
            : $"Xác minh tài khoản {_brandName}";

        var bodyText = lang == "en"
            ? string.Create(CultureInfo.InvariantCulture, $@"Hi {toName},

Thank you for signing up to {_brandName}. To activate your account, please open this link in your browser:

  {url}

This link is single-use and will expire in 24 hours. If you didn't request this, you can ignore this email.

— The {_brandName} team")
            : string.Create(CultureInfo.InvariantCulture, $@"Xin chào {toName},

Cảm ơn bạn đã đăng ký {_brandName}. Để kích hoạt tài khoản, vui lòng mở liên kết sau trong trình duyệt:

  {url}

Liên kết này chỉ dùng được 1 lần và sẽ hết hạn sau 24 giờ. Nếu bạn không yêu cầu, có thể bỏ qua email này.

— Đội ngũ {_brandName}");

        var bodyHtml = lang == "en"
            ? $"<p>Hi {WebEscape(toName)},</p><p>Thank you for signing up to {_brandName}. To activate your account, click the link below:</p><p><a href=\"{WebEscape(url)}\">Verify my email</a></p><p>This link is single-use and will expire in 24 hours. If you didn't request this, you can ignore this email.</p><p>— The {_brandName} team</p>"
            : $"<p>Xin chào {WebEscape(toName)},</p><p>Cảm ơn bạn đã đăng ký {_brandName}. Để kích hoạt tài khoản, hãy bấm vào liên kết bên dưới:</p><p><a href=\"{WebEscape(url)}\">Xác minh email của tôi</a></p><p>Liên kết này chỉ dùng được 1 lần và sẽ hết hạn sau 24 giờ. Nếu bạn không yêu cầu, có thể bỏ qua email này.</p><p>— Đội ngũ {_brandName}</p>";

        return new EmailMessage(toEmail, toName, subject, bodyText, bodyHtml, lang);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Password Reset
    // ─────────────────────────────────────────────────────────────────────────

    public EmailMessage BuildPasswordReset(string toEmail, string toName, string tokenPlaintext, string language)
    {
        var lang = Normalize(language);
        var url = BuildResetPasswordUrl(tokenPlaintext);

        var subject = lang == "en"
            ? $"Reset your {_brandName} password"
            : $"Đặt lại mật khẩu {_brandName}";

        var bodyText = lang == "en"
            ? $"Hi {toName},\n\nWe received a request to reset your password. Open this link (expires in 24 hours):\n\n  {url}\n\nIf you didn't request a reset, ignore this email — your password stays unchanged.\n\n— The {_brandName} team"
            : $"Xin chào {toName},\n\nChúng tôi nhận được yêu cầu đặt lại mật khẩu. Vui lòng mở liên kết (hết hạn sau 24 giờ):\n\n  {url}\n\nNếu bạn không yêu cầu, có thể bỏ qua — mật khẩu của bạn sẽ không thay đổi.\n\n— Đội ngũ {_brandName}";

        var bodyHtml = lang == "en"
            ? $"<p>Hi {WebEscape(toName)},</p><p>We received a request to reset your password.</p><p><a href=\"{WebEscape(url)}\">Reset my password</a></p><p>Link expires in 24 hours. If you didn't request a reset, ignore this email.</p><p>— The {_brandName} team</p>"
            : $"<p>Xin chào {WebEscape(toName)},</p><p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu.</p><p><a href=\"{WebEscape(url)}\">Đặt lại mật khẩu của tôi</a></p><p>Liên kết hết hạn sau 24 giờ. Nếu bạn không yêu cầu, có thể bỏ qua email này.</p><p>— Đội ngũ {_brandName}</p>";

        return new EmailMessage(toEmail, toName, subject, bodyText, bodyHtml, lang);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Admin-issued account
    // ─────────────────────────────────────────────────────────────────────────

    public EmailMessage BuildAdminCreatedAccount(string toEmail, string toName, string initialPassword, string role, string language)
    {
        var lang = Normalize(language);
        var loginUrl = _urls.AppBaseUrl.TrimEnd('/') + "/login";

        var subject = lang == "en"
            ? $"Your {_brandName} account has been created"
            : $"Tài khoản {_brandName} của bạn đã được tạo";

        var bodyText = lang == "en"
            ? $"Hi {toName},\n\nAn administrator created a {role} account for you.\n\n  Email: {toEmail}\n  Temporary password: {initialPassword}\n  Login: {loginUrl}\n\nFor security, please change your password the first time you log in.\n\n— The {_brandName} team"
            : $"Xin chào {toName},\n\nQuản trị viên đã tạo tài khoản {role} cho bạn.\n\n  Email: {toEmail}\n  Mật khẩu tạm: {initialPassword}\n  Đăng nhập: {loginUrl}\n\nVì lý do bảo mật, vui lòng đổi mật khẩu ngay khi đăng nhập lần đầu.\n\n— Đội ngũ {_brandName}";

        var bodyHtml = lang == "en"
            ? $"<p>Hi {WebEscape(toName)},</p><p>An administrator created a <strong>{role}</strong> account for you.</p><ul><li>Email: {WebEscape(toEmail)}</li><li>Temporary password: <code>{WebEscape(initialPassword)}</code></li><li><a href=\"{WebEscape(loginUrl)}\">Sign in</a></li></ul><p>For security, please change your password the first time you log in.</p><p>— The {_brandName} team</p>"
            : $"<p>Xin chào {WebEscape(toName)},</p><p>Quản trị viên đã tạo tài khoản <strong>{role}</strong> cho bạn.</p><ul><li>Email: {WebEscape(toEmail)}</li><li>Mật khẩu tạm: <code>{WebEscape(initialPassword)}</code></li><li><a href=\"{WebEscape(loginUrl)}\">Đăng nhập</a></li></ul><p>Vì lý do bảo mật, vui lòng đổi mật khẩu ngay khi đăng nhập lần đầu.</p><p>— Đội ngũ {_brandName}</p>";

        return new EmailMessage(toEmail, toName, subject, bodyText, bodyHtml, lang);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private string BuildVerifyEmailUrl(string token) =>
        $"{_urls.AppBaseUrl.TrimEnd('/')}/auth/verify-email?token={Uri.EscapeDataString(token)}";

    private string BuildResetPasswordUrl(string token) =>
        $"{_urls.AppBaseUrl.TrimEnd('/')}/auth/reset-password?token={Uri.EscapeDataString(token)}";

    private static string Normalize(string language) =>
        (language ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "en" => "en",
            _ => "vi",
        };

    /// <summary>Minimal HTML escape for body inserts. Names + emails are not user-controlled HTML.</summary>
    private static string WebEscape(string value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("&", "&amp;", StringComparison.Ordinal)
                   .Replace("<", "&lt;", StringComparison.Ordinal)
                   .Replace(">", "&gt;", StringComparison.Ordinal)
                   .Replace("\"", "&quot;", StringComparison.Ordinal);
}
