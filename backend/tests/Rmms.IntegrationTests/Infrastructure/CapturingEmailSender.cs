using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Rmms.Application.Common.Abstractions;

namespace Rmms.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IEmailSender"/> that captures every message in memory so
/// integration tests can extract verification / reset tokens from the body
/// (mirrors how the smoke script greps the console log, but deterministic).
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Extract the <c>token=...</c> value from the most recent email to <paramref name="email"/>.</summary>
    public string? LatestTokenFor(string email)
    {
        var msg = _messages
            .Where(m => string.Equals(m.ToEmail, email, StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();
        if (msg is null)
        {
            return null;
        }

        var match = Regex.Match(msg.BodyText, "token=([A-Za-z0-9_\\-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    public void Clear() => _messages.Clear();
}
