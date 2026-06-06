using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Approvals;

/// <summary>
/// Generic approval record (M09). One row gates one request (work_schedule / OT /
/// leave / visit plan) and is routed to a single approver (PG→Leader BR-405,
/// Leader→BUH BR-406). Simple state machine — Pending → Approved | Rejected |
/// Overridden (no workflow-core library in Phase 1, per M09 notes).
/// </summary>
public sealed class Approval : AuditableEntity, IAggregateRoot
{
    public ApprovalEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid RequesterId { get; private set; }

    /// <summary>The user who must decide (Leader or BUH).</summary>
    public Guid ApproverId { get; private set; }
    public UserRole ApproverRole { get; private set; }

    public ApprovalStatus Status { get; private set; }
    public string? DecisionReason { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public ApprovalDecisionVia? DecidedVia { get; private set; }

    /// <summary>Admin who overrode the decision (BR-408). Null unless overridden.</summary>
    public Guid? OverriddenBy { get; private set; }
    public string? OverrideReason { get; private set; }

    private Approval() { } // EF Core

    public static Approval Create(
        ApprovalEntityType entityType,
        Guid entityId,
        Guid requesterId,
        Guid approverId,
        UserRole approverRole)
    {
        if (entityId == Guid.Empty) throw new ArgumentException("Entity id is required.", nameof(entityId));
        if (requesterId == Guid.Empty) throw new ArgumentException("Requester id is required.", nameof(requesterId));
        if (approverId == Guid.Empty) throw new ArgumentException("Approver id is required.", nameof(approverId));
        if (approverRole is not (UserRole.Leader or UserRole.Buh))
            throw new ArgumentException("Approver role must be Leader or BUH.", nameof(approverRole));

        return new Approval
        {
            EntityType = entityType,
            EntityId = entityId,
            RequesterId = requesterId,
            ApproverId = approverId,
            ApproverRole = approverRole,
            Status = ApprovalStatus.Pending,
        };
    }

    public bool IsPending => Status == ApprovalStatus.Pending;

    public void Approve(Guid approverId, ApprovalDecisionVia via, DateTimeOffset now)
    {
        EnsurePending();
        Status = ApprovalStatus.Approved;
        DecisionReason = null;
        DecidedAt = now;
        DecidedVia = via;
        Touch(approverId, now);
    }

    public void Reject(Guid approverId, string reason, ApprovalDecisionVia via, DateTimeOffset now)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reject reason is required.", nameof(reason));
        Status = ApprovalStatus.Rejected;
        DecisionReason = reason.Trim();
        DecidedAt = now;
        DecidedVia = via;
        Touch(approverId, now);
    }

    /// <summary>
    /// Admin override (BR-408): may override a pending OR already-decided approval,
    /// but never one already overridden (idempotency / concurrency guard → 409).
    /// </summary>
    public void Override(Guid adminId, string reason, DateTimeOffset now)
    {
        if (Status == ApprovalStatus.Overridden)
            throw new InvalidOperationException("Approval is already overridden.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Override reason is required.", nameof(reason));
        Status = ApprovalStatus.Overridden;
        OverriddenBy = adminId;
        OverrideReason = reason.Trim();
        DecidedAt = now;
        DecidedVia = ApprovalDecisionVia.Web;
        Touch(adminId, now);
    }

    private void EnsurePending()
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Only a pending approval can be decided.");
    }

    private void Touch(Guid actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId;
    }
}
