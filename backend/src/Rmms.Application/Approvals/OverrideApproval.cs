using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Approvals;

/// <summary>
/// Admin override of an approval (M09, BR-408 / AC-19). Requires a reason and is
/// always audited. Overriding an already-overridden approval returns 409 (first wins).
/// </summary>
public sealed record OverrideApprovalCommand(Guid Id, Guid AdminId, string Reason) : IRequest<Result>;

internal sealed class OverrideApprovalCommandHandler : IRequestHandler<OverrideApprovalCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public OverrideApprovalCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(OverrideApprovalCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure(Error.Validation(ErrorCodes.RejectReasonRequired, "Vui lòng nhập lý do override."));

        var approval = await _db.Approvals.FirstOrDefaultAsync(a => a.Id == command.Id, ct);
        if (approval is null)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu phê duyệt."));
        if (approval.Status == ApprovalStatus.Overridden)
            return Result.Failure(Error.Conflict(ErrorCodes.Conflict, "Yêu cầu đã được override."));

        approval.Override(command.AdminId, command.Reason, _clock.UtcNow);
        await _audit.RecordAsync(AuditAction.ApprovalOverridden, "approval", approval.Id,
            new { approval.EntityType, approval.EntityId, reason = command.Reason.Trim() }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
