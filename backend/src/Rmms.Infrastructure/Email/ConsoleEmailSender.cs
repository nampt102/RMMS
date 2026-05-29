using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Email;

/// <summary>
/// Dev / Test email sender — logs the email content via Serilog instead of dispatching.
/// Activated when <c>Email:Provider=Console</c> in configuration.
///
/// Useful for E2E tests: a test helper can scrape the log to extract the verification token.
/// </summary>
internal sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ConsoleEmailSender] To={ToEmail} ({ToName}) Lang={Language} Subject={Subject}\n--- TEXT BODY ---\n{BodyText}\n--- END ---",
            message.ToEmail,
            message.ToName,
            message.Language,
            message.Subject,
            message.BodyText);

        return Task.CompletedTask;
    }
}
