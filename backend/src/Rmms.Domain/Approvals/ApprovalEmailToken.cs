using Rmms.Domain.Common;

namespace Rmms.Domain.Approvals;

/// <summary>
/// One-time, time-boxed token backing a BUH email-link decision (M09, BR-407 /
/// AC-18). The signed token travels in the email URL; only its SHA-256 hash is
/// persisted. TTL 24h, single use, and IP/UA are captured on consume.
/// </summary>
public sealed class ApprovalEmailToken : Entity
{
    public Guid ApprovalId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ApprovalEmailToken() { } // EF Core

    public static ApprovalEmailToken Issue(Guid approvalId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (approvalId == Guid.Empty) throw new ArgumentException("Approval id is required.", nameof(approvalId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new ApprovalEmailToken
        {
            ApprovalId = approvalId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public bool IsUsed => UsedAt is not null;
    public bool IsValid(DateTimeOffset now) => !IsUsed && !IsExpired(now);

    public void MarkUsed(DateTimeOffset at, string? ipAddress, string? userAgent)
    {
        if (IsUsed) throw new InvalidOperationException("Approval email token already used.");
        UsedAt = at;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
