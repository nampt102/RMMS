using Rmms.Domain.Enums;

namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// In-process producer API for the M09 approval engine. Other modules call this to
/// enqueue an approval routed to a Leader (PG→Leader, BR-405) or BUH (Leader→BUH,
/// BR-406). For a BUH approver it also issues a signed email-link token and sends
/// the decision email (BR-407). The caller owns the unit of work (SaveChanges).
/// </summary>
public interface IApprovalService
{
    Task<Guid> CreateAsync(
        ApprovalEntityType entityType,
        Guid entityId,
        Guid requesterId,
        Guid approverId,
        UserRole approverRole,
        CancellationToken ct = default);
}
