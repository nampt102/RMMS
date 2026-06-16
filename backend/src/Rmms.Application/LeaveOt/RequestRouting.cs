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
/// Resolves the approver for a request raised by <paramref name="ownerId"/> (shared by M07
/// schedule, M08 leave/OT, M11 visit plan → M09). Routing per the approval decision table:
///   - PG     → their active Leader (BR-405).
///   - Leader → a BUH (BR-406). BUH is a domain-wide role (PRD: not partitioned by area, not
///              assigned per Leader), so the request routes to the active BUH — deterministic
///              earliest-created when more than one exists (effectively a single BUH in Phase 1).
///   - BUH / Admin requests are not self-routed in Phase 1.
/// Returns null when there is no routable approver (request stays pending, no approval row).
/// </summary>
internal static class RequestRouting
{
    public static async Task<(Guid ApproverId, UserRole Role)?> ResolveApproverAsync(
        IAppDbContext db, Guid ownerId, CancellationToken ct)
    {
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, ct);
        if (owner is null) return null;

        switch (owner.Role)
        {
            case UserRole.Pg:
                var leaderId = await db.UserLeaderAssignments
                    .Where(a => a.PgUserId == ownerId && a.EffectiveTo == null)
                    .Select(a => a.LeaderUserId)
                    .FirstOrDefaultAsync(ct);
                return leaderId == Guid.Empty ? null : (leaderId, UserRole.Leader);

            case UserRole.Leader:
                var buhId = await ResolveBuhApproverAsync(db, ct);
                return buhId is { } b ? (b, UserRole.Buh) : null;

            default:
                return null;
        }
    }

    /// <summary>The active BUH a Leader request routes to (BR-406); earliest-created if several.</summary>
    private static async Task<Guid?> ResolveBuhApproverAsync(IAppDbContext db, CancellationToken ct)
    {
        var id = await db.Users
            .Where(u => u.Role == UserRole.Buh && u.Status == UserStatus.Active)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        return id == Guid.Empty ? null : id;
    }
}
