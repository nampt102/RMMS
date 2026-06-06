using Rmms.Application.Common;
using Rmms.Domain.Approvals;

namespace Rmms.Application.Approvals;

/// <summary>Approval row projected for list + detail (M09).</summary>
public sealed record ApprovalDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    Guid RequesterId,
    string RequesterName,
    Guid ApproverId,
    string ApproverRole,
    string Status,
    string? DecisionReason,
    DateTimeOffset? DecidedAt,
    string? DecidedVia,
    Guid? OverriddenBy,
    string? OverrideReason,
    DateTimeOffset CreatedAt);

/// <summary>Result of consuming a BUH email link (AC-18) — drives the landing page.</summary>
public sealed record EmailActionResultDto(string Status, string EntityType);

/// <summary>Preview of an email-link target without consuming it (landing page render).</summary>
public sealed record EmailActionPreviewDto(
    bool Valid,
    bool Expired,
    bool Used,
    bool AlreadyDecided,
    string? Status,
    string? EntityType,
    IReadOnlyList<string> ActionOptions);

internal static class ApprovalMapper
{
    public static ApprovalDto ToDto(Approval a, string requesterName) => new(
        Id: a.Id,
        EntityType: a.EntityType.ToSnakeCase(),
        EntityId: a.EntityId,
        RequesterId: a.RequesterId,
        RequesterName: requesterName,
        ApproverId: a.ApproverId,
        ApproverRole: a.ApproverRole.ToString().ToLowerInvariant(),
        Status: a.Status.ToSnakeCase(),
        DecisionReason: a.DecisionReason,
        DecidedAt: a.DecidedAt,
        DecidedVia: a.DecidedVia?.ToSnakeCase(),
        OverriddenBy: a.OverriddenBy,
        OverrideReason: a.OverrideReason,
        CreatedAt: a.CreatedAt);
}
