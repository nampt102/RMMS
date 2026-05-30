using System.Collections.Concurrent;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>
/// Captures audit calls in memory so tests can assert exactly which actions were emitted.
/// Does NOT actually write to the DbContext (saving is mocked at the test level).
/// </summary>
internal sealed class InMemoryAuditLogger : IAuditLogger
{
    public ConcurrentBag<AuditCall> Calls { get; } = new();

    public Task RecordAsync(string action, string targetEntity, Guid? targetId, object? metadata = null, CancellationToken ct = default)
    {
        Calls.Add(new AuditCall(action, targetEntity, targetId, metadata));
        return Task.CompletedTask;
    }

    public sealed record AuditCall(string Action, string TargetEntity, Guid? TargetId, object? Metadata);
}
