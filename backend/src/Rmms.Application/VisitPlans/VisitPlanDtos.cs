using Rmms.Application.Common;
using Rmms.Domain.VisitPlan;

namespace Rmms.Application.VisitPlans;

/// <summary>One planned store visit projected for list/detail (M11).</summary>
public sealed record VisitPlanItemDto(
    Guid Id,
    Guid StoreId,
    Guid FormId,
    int Ordering,
    DateTimeOffset? ExecutedAt,
    Guid? FormSubmissionId);

/// <summary>Visit plan projected for list/detail (M11).</summary>
public sealed record VisitPlanDto(
    Guid Id,
    Guid LeaderUserId,
    DateOnly VisitDate,
    string? Notes,
    string Status,
    Guid? ApprovalId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VisitPlanItemDto> Items,
    string? LeaderName = null);

internal static class VisitPlanMapper
{
    public static VisitPlanDto ToDto(VisitPlan p, string? leaderName = null) => new(
        p.Id,
        p.LeaderUserId,
        p.VisitDate,
        p.Notes,
        p.Status.ToSnakeCase(),
        p.ApprovalId,
        p.CreatedAt,
        p.Items
            .OrderBy(i => i.Ordering)
            .Select(i => new VisitPlanItemDto(i.Id, i.StoreId, i.FormId, i.Ordering, i.ExecutedAt, i.FormSubmissionId))
            .ToList(),
        leaderName);
}
