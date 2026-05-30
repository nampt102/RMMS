using System.Collections.Concurrent;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

internal sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentBag<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
