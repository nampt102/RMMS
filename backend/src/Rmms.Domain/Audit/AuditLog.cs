using System.Net;
using Rmms.Domain.Common;

namespace Rmms.Domain.Audit;

/// <summary>
/// Append-only audit log per <c>06-business-rules.md</c> CR-1.
///
/// At the DATABASE LEVEL this table is enforced append-only:
///   <c>REVOKE UPDATE, DELETE ON audit_log FROM rmms_app;</c>
/// (applied in the initial migration; see also ADR-004 commentary).
///
/// Not <see cref="AuditableEntity"/> — IS the audit log. Recursive auditing makes no sense.
///
/// <see cref="Metadata"/> is a JSON-serialized blob storing module-specific context
/// (M01: <c>{ ip, ua, device_id }</c>; M02 will add <c>{ approver_id, request_id }</c>; etc.).
/// Schemaless intentionally — keeps the table stable as new modules emit new shapes.
/// </summary>
public sealed class AuditLog : Entity
{
    /// <summary>
    /// Who triggered the action. Null if the actor is not a registered user
    /// (e.g., failed login before user is identified, or a system job).
    /// </summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>Canonical action code from <see cref="Enums.AuditAction"/>.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Aggregate type that the action affected, e.g., <c>user</c>, <c>device</c>, <c>form</c>.</summary>
    public string TargetEntity { get; private set; } = string.Empty;

    /// <summary>Specific aggregate id, when applicable.</summary>
    public Guid? TargetId { get; private set; }

    public IPAddress? IpAddress { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;

    /// <summary>JSON-serialized blob; never null (defaults to <c>{}</c>).</summary>
    public string Metadata { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Record(string action, string targetEntity, Guid? targetId, Guid? actorUserId, IPAddress? ip, string userAgent, string? metadataJson, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEntity);

        return new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action.Trim(),
            TargetEntity = targetEntity.Trim().ToLowerInvariant(),
            TargetId = targetId,
            IpAddress = ip,
            UserAgent = (userAgent ?? string.Empty).Trim(),
            Metadata = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson,
            CreatedAt = at,
        };
    }
}
