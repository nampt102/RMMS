using Rmms.Application.Common.Abstractions;
using Rmms.Application.Email;

namespace Rmms.UnitTests.Common;

/// <summary>
/// Deterministic email body builder — tests can scrape the body for the token plaintext.
/// </summary>
internal sealed class FakeTemplateRenderer : IEmailTemplateRenderer
{
    public EmailMessage BuildVerifyEmail(string toEmail, string toName, string tokenPlaintext, string language) =>
        new(toEmail, toName,
            Subject: "[VERIFY] " + toEmail,
            BodyText: $"verify token={tokenPlaintext}",
            BodyHtml: $"<a>verify token={tokenPlaintext}</a>",
            Language: language);

    public EmailMessage BuildPasswordReset(string toEmail, string toName, string tokenPlaintext, string language) =>
        new(toEmail, toName,
            Subject: "[RESET] " + toEmail,
            BodyText: $"reset token={tokenPlaintext}",
            BodyHtml: $"<a>reset token={tokenPlaintext}</a>",
            Language: language);

    public EmailMessage BuildAdminCreatedAccount(string toEmail, string toName, string initialPassword, string role, string language) =>
        new(toEmail, toName,
            Subject: "[ADMIN] " + toEmail,
            BodyText: $"role={role} pwd={initialPassword}",
            BodyHtml: $"<p>role={role} pwd={initialPassword}</p>",
            Language: language);
}
