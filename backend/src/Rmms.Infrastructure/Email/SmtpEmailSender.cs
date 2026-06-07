using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Email;

/// <summary>
/// SMTP email sender (e.g. Gmail via an App Password) — activated when
/// <c>Email:Provider=Smtp</c>. STARTTLS on port 587. Sends an HTML body with a plain-text
/// alternate. Logs + no-throw when credentials are missing so email never breaks the caller.
/// For Gmail: host <c>smtp.gmail.com</c>, port 587, user = the Gmail address, password = a
/// 16-char App Password (spaces are stripped automatically).
/// </summary>
internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var user = _options.SmtpUser;
        var password = _options.SmtpPassword.Replace(" ", string.Empty); // Gmail shows the app password in 4 groups
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "[SMTP] Missing credentials — skipping send. To={ToEmail} Subject={Subject}",
                message.ToEmail, message.Subject);
            return;
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(user, password),
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = message.BodyHtml,
            IsBodyHtml = true,
        };
        mail.To.Add(new MailAddress(message.ToEmail, message.ToName));
        // Plain-text alternate for clients that don't render HTML.
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.BodyText, null, "text/plain"));

        try
        {
            await client.SendMailAsync(mail, ct);
            _logger.LogInformation("[SMTP] Sent To={ToEmail} Subject={Subject}", message.ToEmail, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMTP] Send failed To={ToEmail} Subject={Subject}", message.ToEmail, message.Subject);
        }
    }
}
