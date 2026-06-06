using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Approvals;

/// <summary>Approve a pending approval (M09 AC-17). The caller must be the routed approver.</summary>
public sealed record ApproveApprovalCommand(Guid Id, Guid ApproverId, ApprovalDecisionVia Via)
    : IRequest<Result>;

/// <summary>Reject a pending approval — reason required (BR-404).</summary>
public sealed record RejectApprovalCommand(Guid Id, Guid ApproverId, string Reason, ApprovalDecisionVia Via)
    : IRequest<Result>;

internal sealed class ApproveApprovalCommandHandler : IRequestHandler<ApproveApprovalCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ApproveApprovalCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ApproveApprovalCommand command, CancellationToken ct)
    {
        var approval = await _db.Approvals.FirstOrDefaultAsync(a => a.Id == command.Id, ct);
        var guard = Guard(approval, command.ApproverId);
        if (guard is not null) return guard;

        approval!.Approve(command.ApproverId, command.Via, _clock.UtcNow);
        await _audit.RecordAsync(AuditAction.ApprovalApproved, "approval", approval.Id,
            new { approval.EntityType, approval.EntityId, via = command.Via.ToSnakeCase() }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    internal static Result? Guard(Approval? approval, Guid approverId)
    {
        if (approval is null)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu phê duyệt."));
        if (approval.ApproverId != approverId)
            return Result.Failure(Error.Forbidden(ErrorCodes.NotApprover, "Bạn không phải người duyệt yêu cầu này."));
        if (!approval.IsPending)
            return Result.Failure(Error.Conflict(ErrorCodes.ApprovalNotPending, "Yêu cầu không ở trạng thái chờ duyệt."));
        return null;
    }
}

internal sealed class RejectApprovalCommandHandler : IRequestHandler<RejectApprovalCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public RejectApprovalCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(RejectApprovalCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure(Error.Validation(ErrorCodes.RejectReasonRequired, "Vui lòng nhập lý do từ chối."));

        var approval = await _db.Approvals.FirstOrDefaultAsync(a => a.Id == command.Id, ct);
        var guard = ApproveApprovalCommandHandler.Guard(approval, command.ApproverId);
        if (guard is not null) return guard;

        approval!.Reject(command.ApproverId, command.Reason, command.Via, _clock.UtcNow);
        await _audit.RecordAsync(AuditAction.ApprovalRejected, "approval", approval.Id,
            new { approval.EntityType, approval.EntityId, reason = command.Reason.Trim(), via = command.Via.ToSnakeCase() }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
