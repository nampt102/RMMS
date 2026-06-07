using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.LeaveOt;

/// <summary>Withdraw a still-pending leave request (M08). Also clears its pending approval.</summary>
public sealed record WithdrawLeaveRequestCommand(Guid Id, Guid UserId) : IRequest<Result>;

internal sealed class WithdrawLeaveRequestCommandHandler : IRequestHandler<WithdrawLeaveRequestCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public WithdrawLeaveRequestCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(WithdrawLeaveRequestCommand command, CancellationToken ct)
    {
        var request = await _db.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, ct);
        if (request is null)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn nghỉ."));
        if (!request.IsPending)
            return Result.Failure(Error.Conflict(ErrorCodes.Conflict, "Chỉ đơn đang chờ duyệt mới có thể thu hồi."));

        // Clear the linked pending approval so it leaves the approver's queue (soft-delete).
        if (request.ApprovalId is { } approvalId)
        {
            var approval = await _db.Approvals.FirstOrDefaultAsync(
                a => a.Id == approvalId && a.Status == ApprovalStatus.Pending, ct);
            if (approval is not null) _db.Approvals.Remove(approval);
        }

        _db.LeaveRequests.Remove(request); // soft-delete (ADR-004)

        await _audit.RecordAsync(AuditAction.LeaveWithdrawn, "leave_request", request.Id,
            new { request.UserId }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
