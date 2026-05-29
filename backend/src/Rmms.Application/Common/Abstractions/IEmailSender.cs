namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Sends transactional emails (verify-email, password reset, admin-created account password, …).
///
/// Implementations:
///   - <c>Rmms.Infrastructure.Email.ConsoleEmailSender</c> — Dev (logs to console + Serilog).
///   - <c>Rmms.Infrastructure.Email.SendGridEmailSender</c> — Staging / Prod (wired in Sprint 01 Day 8).
///
/// Templates are simple string interpolation in Sprint 01; HTML templating engine
/// (e.g., RazorLight / Scriban) is deferred to Sprint 03 when M14 News needs HTML newsletters.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string BodyText,
    string BodyHtml,
    /// <summary>Language code for the email body — <c>vi</c> or <c>en</c>.</summary>
    string Language = "vi");
