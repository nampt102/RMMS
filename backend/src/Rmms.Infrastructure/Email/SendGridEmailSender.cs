using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Rmms.Infrastructure.Email;

/// <summary>
/// Transactional email via Twilio SendGrid (Staging / Prod). Activated when
/// <c>Email:Provider=SendGrid</c> and an API key is configured. Sends both a text and
/// HTML part; falls back to logging (and does not throw) if the key is missing so a
/// misconfiguration never breaks the calling flow (e.g. registration / approval).
/// </summary>
internal sealed class SendGridEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IOptions<EmailOptions> options, ILogger<SendGridEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "[SendGrid] No API key configured — skipping send. To={ToEmail} Subject={Subject}",
                message.ToEmail, message.Subject);
            return;
        }

        var client = new SendGridClient(_options.ApiKey);
        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(message.ToEmail, message.ToName);
        var mail = MailHelper.CreateSingleEmail(
            from, to, message.Subject, message.BodyText, message.BodyHtml);

        var response = await client.SendEmailAsync(mail, ct);
        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError(
                "[SendGrid] Send failed {Status} To={ToEmail} Subject={Subject} Body={Body}",
                (int)response.StatusCode, message.ToEmail, message.Subject, body);
        }
        else
        {
            _logger.LogInformation(
                "[SendGrid] Sent {Status} To={ToEmail} Subject={Subject}",
                (int)response.StatusCode, message.ToEmail, message.Subject);
        }
    }
}
