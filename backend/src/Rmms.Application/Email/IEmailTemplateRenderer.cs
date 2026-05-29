using Rmms.Application.Common.Abstractions;

namespace Rmms.Application.Email;

/// <summary>
/// Builds <see cref="EmailMessage"/> instances for transactional emails (verify-email,
/// password reset, admin-issued accounts, …) with vi/en locale support.
///
/// Implementation lives in Infrastructure (<c>Rmms.Infrastructure.Email.EmailTemplateRenderer</c>)
/// so it can use config (base URL, brand name) and IO without polluting the Application layer.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>Build verify-email message with link <c>{baseUrl}/auth/verify-email?token=...</c>.</summary>
    EmailMessage BuildVerifyEmail(string toEmail, string toName, string tokenPlaintext, string language);

    /// <summary>Build password-reset message with link <c>{baseUrl}/auth/reset-password?token=...</c>.</summary>
    EmailMessage BuildPasswordReset(string toEmail, string toName, string tokenPlaintext, string language);

    /// <summary>Build admin-created-account message (initial password inside).</summary>
    EmailMessage BuildAdminCreatedAccount(string toEmail, string toName, string initialPassword, string role, string language);
}
