using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Application.LeaveOt;

/// <summary>Routes a new request to the M09 approval engine and links the created approval.</summary>
internal static class LeaveOtProducer
{
    public static async Task RouteAsync(
        IAppDbContext db, IApprovalService approvals, ApprovalEntityType entityType,
        Guid entityId, Guid ownerId, Action<Guid> linkApproval, CancellationToken ct)
    {
        var route = await RequestRouting.ResolveApproverAsync(db, ownerId, ct);
        if (route is null) return; // no routable approver → request stays pending, no approval row
        var approvalId = await approvals.CreateAsync(entityType, entityId, ownerId, route.Value.ApproverId, route.Value.Role, ct);
        linkApproval(approvalId);
    }
}

/// <summary>
/// Resolves the approver for a request raised by <paramref name="ownerId"/> (M08 → M09).
/// Phase 1: PG → their active Leader (BR-405). Leader → BUH (BR-406) is skipped until a
/// Leader↔BUH assignment exists. Returns null when there is no routable approver.
/// </summary>
internal static class RequestRouting
{
    public static async Task<(Guid ApproverId, UserRole Role)?> ResolveApproverAsync(
        IAppDbContext db, Guid ownerId, CancellationToken ct)
    {
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, ct);
        if (owner is null || owner.Role != UserRole.Pg) return null;

        var leaderId = await db.UserLeaderAssignments
            .Where(a => a.PgUserId == ownerId && a.EffectiveTo == null)
            .Select(a => a.LeaderUserId)
            .FirstOrDefaultAsync(ct);

        return leaderId == Guid.Empty ? null : (leaderId, UserRole.Leader);
    }
}
