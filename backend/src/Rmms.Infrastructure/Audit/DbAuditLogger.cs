using System.Text.Json;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Audit;

namespace Rmms.Infrastructure.Audit;

/// <summary>
/// Persists <see cref="AuditLog"/> rows via <see cref="IAppDbContext"/>.
///
/// Does NOT call <c>SaveChangesAsync</c> — caller's UoW commits along with the business change.
/// This is intentional: audit + business state must commit (or roll back) atomically per CR-1.
/// </summary>
internal sealed class DbAuditLogger : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClientContext _clientContext;
    private readonly IDateTimeProvider _clock;

    public DbAuditLogger(IAppDbContext db, ICurrentUser currentUser, IClientContext clientContext, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clientContext = clientContext;
        _clock = clock;
    }

    public Task RecordAsync(string action, string targetEntity, Guid? targetId, object? metadata = null, CancellationToken ct = default)
    {
        var metadataJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata, JsonOptions);

        var row = AuditLog.Record(
            action: action,
            targetEntity: targetEntity,
            targetId: targetId,
            actorUserId: _currentUser.UserId,
            ip: _clientContext.IpAddress,
            userAgent: _clientContext.UserAgent,
            metadataJson: metadataJson,
            at: _clock.UtcNow);

        _db.AuditLogs.Add(row);
        return Task.CompletedTask;
    }
}
