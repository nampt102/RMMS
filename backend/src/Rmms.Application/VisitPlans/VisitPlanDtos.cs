using Rmms.Application.Common;
using Rmms.Domain.VisitPlan;

namespace Rmms.Application.VisitPlans;

/// <summary>One planned store visit projected for list/detail (M11). Store/form names are
/// resolved by the query handler for display; they are null when no lookup was supplied.</summary>
public sealed record VisitPlanItemDto(
    Guid Id,
    Guid StoreId,
    string? StoreName,
    Guid FormId,
    string? FormName,
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

/// <summary>Name lookups for projecting a plan (store id → name, form id → name).</summary>
internal sealed record VisitPlanLookups(
    IReadOnlyDictionary<Guid, string>? Stores = null,
    IReadOnlyDictionary<Guid, string>? Forms = null);

internal static class VisitPlanMapper
{
    public static VisitPlanDto ToDto(VisitPlan p, string? leaderName = null, VisitPlanLookups? lookups = null)
    {
        string? StoreName(Guid id) => lookups?.Stores is { } s && s.TryGetValue(id, out var n) ? n : null;
        string? FormName(Guid id) => lookups?.Forms is { } f && f.TryGetValue(id, out var n) ? n : null;

        return new VisitPlanDto(
            p.Id,
            p.LeaderUserId,
            p.VisitDate,
            p.Notes,
            p.Status.ToSnakeCase(),
            p.ApprovalId,
            p.CreatedAt,
            p.Items
                .OrderBy(i => i.Ordering)
                .Select(i => new VisitPlanItemDto(
                    i.Id, i.StoreId, StoreName(i.StoreId), i.FormId, FormName(i.FormId),
                    i.Ordering, i.ExecutedAt, i.FormSubmissionId))
                .ToList(),
            leaderName);
    }
}
