using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Application.VisitPlans;

/// <summary>Routes a new visit plan to the M09 approval engine (Leader→BUH, BR-406) and links it.</summary>
internal static class VisitPlanProducer
{
    public static async Task RouteAsync(
        IAppDbContext db, IApprovalService approvals, Guid planId, Guid leaderId,
        Action<Guid> linkApproval, CancellationToken ct)
    {
        var buhId = await VisitPlanRouting.ResolveBuhApproverAsync(db, ct);
        if (buhId is null) return; // no routable BUH → plan stays pending, no approval row (Admin may override)

        var approvalId = await approvals.CreateAsync(
            ApprovalEntityType.VisitPlan, planId, leaderId, buhId.Value, UserRole.Buh, ct);
        linkApproval(approvalId);
    }
}

/// <summary>
/// Resolves the BUH who approves a Leader's visit plan (BR-406). Phase 1 is single-customer
/// (no multi-tenancy, no explicit Leader↔BUH mapping table yet), so the approver is "the" active
/// BUH; when more than one exists we pick the earliest-created deterministically. Returns null
/// when there is no active BUH — the plan then stays pending until an Admin override.
///
/// Phase 2 refinement: an explicit Leader↔BUH assignment (mirroring user_leader_assignments).
/// </summary>
internal static class VisitPlanRouting
{
    public static async Task<Guid?> ResolveBuhApproverAsync(IAppDbContext db, CancellationToken ct)
    {
        var buhId = await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Buh && u.Status == UserStatus.Active)
            .OrderBy(u => u.CreatedAt)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        return buhId;
    }
}
