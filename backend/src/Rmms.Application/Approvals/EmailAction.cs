using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Approvals;

/// <summary>
/// Preview a BUH email link without consuming it — drives the friendly landing page
/// (M09, AC-18). Never throws on a bad/expired/used token; reports it via flags.
/// </summary>
public sealed record EmailActionPreviewQuery(string Token) : IRequest<Result<EmailActionPreviewDto>>;

/// <summary>
/// Consume a BUH email link and record the decision (AC-18). One-time use; logs IP/UA.
/// </summary>
public sealed record EmailActionConfirmCommand(string Token, string Action, string? Reason, string? Ip, string? UserAgent)
    : IRequest<Result<EmailActionResultDto>>;

internal sealed class EmailActionPreviewQueryHandler
    : IRequestHandler<EmailActionPreviewQuery, Result<EmailActionPreviewDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalTokenService _tokens;
    private readonly IDateTimeProvider _clock;

    public EmailActionPreviewQueryHandler(IAppDbContext db, IApprovalTokenService tokens, IDateTimeProvider clock)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
    }

    public async ValueTask<Result<EmailActionPreviewDto>> Handle(EmailActionPreviewQuery query, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var payload = _tokens.Verify(query.Token, now);
        var hash = OpaqueToken.Hash(query.Token);
        var row = await _db.ApprovalEmailTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (row is null)
            return Result.Success(new EmailActionPreviewDto(false, false, false, false, null, null, Array.Empty<string>()));

        var used = row.IsUsed;
        var expired = row.IsExpired(now);
        var approval = await _db.Approvals.AsNoTracking().FirstOrDefaultAsync(a => a.Id == row.ApprovalId, ct);
        var alreadyDecided = approval is not null && approval.Status != ApprovalStatus.Pending;
        var actions = payload?.ActionOptions ?? new[] { "approve", "reject" };
        var valid = payload is not null && !used && !expired && !alreadyDecided;

        return Result.Success(new EmailActionPreviewDto(
            Valid: valid,
            Expired: expired,
            Used: used,
            AlreadyDecided: alreadyDecided,
            Status: approval?.Status.ToSnakeCase(),
            EntityType: approval?.EntityType.ToSnakeCase(),
            ActionOptions: actions));
    }
}

internal sealed class EmailActionConfirmCommandHandler
    : IRequestHandler<EmailActionConfirmCommand, Result<EmailActionResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalTokenService _tokens;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public EmailActionConfirmCommandHandler(IAppDbContext db, IApprovalTokenService tokens, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _tokens = tokens;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<EmailActionResultDto>> Handle(EmailActionConfirmCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var action = command.Action?.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject"))
            return Fail(Error.Validation(ErrorCodes.ValidationFailed, "Hành động không hợp lệ."));

        var hash = OpaqueToken.Hash(command.Token);
        var row = await _db.ApprovalEmailTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (row is null)
            return Fail(Error.Validation(ErrorCodes.TokenInvalid, "Liên kết không hợp lệ."));
        if (row.IsUsed)
            return Fail(Error.Validation(ErrorCodes.EmailTokenUsed, "Liên kết đã được sử dụng."));
        if (row.IsExpired(now))
            return Fail(Error.Validation(ErrorCodes.EmailTokenExpired, "Liên kết đã hết hạn."));

        var payload = _tokens.Verify(command.Token, now);
        if (payload is null)
            return Fail(Error.Validation(ErrorCodes.TokenInvalid, "Liên kết không hợp lệ."));

        var approval = await _db.Approvals.FirstOrDefaultAsync(a => a.Id == row.ApprovalId, ct);
        if (approval is null)
            return Fail(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu phê duyệt."));
        if (approval.ApproverId != payload.ApproverId)
            return Fail(Error.Forbidden(ErrorCodes.NotApprover, "Liên kết không khớp người duyệt."));
        if (!approval.IsPending)
            return Fail(Error.Conflict(ErrorCodes.ApprovalNotPending, "Yêu cầu đã được quyết định."));

        if (action == "reject")
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
                return Fail(Error.Validation(ErrorCodes.RejectReasonRequired, "Vui lòng nhập lý do từ chối."));
            approval.Reject(approval.ApproverId, command.Reason, ApprovalDecisionVia.EmailLink, now);
            await ScheduleApprovalSync.ApplyToScheduleAsync(_db, approval, approve: false, command.Reason, approval.ApproverId, now, ct);
        }
        else
        {
            approval.Approve(approval.ApproverId, ApprovalDecisionVia.EmailLink, now);
            await ScheduleApprovalSync.ApplyToScheduleAsync(_db, approval, approve: true, reason: null, approval.ApproverId, now, ct);
        }

        row.MarkUsed(now, command.Ip, command.UserAgent);

        var auditAction = action == "reject" ? AuditAction.ApprovalRejected : AuditAction.ApprovalApproved;
        await _audit.RecordAsync(auditAction, "approval", approval.Id,
            new { approval.EntityType, approval.EntityId, via = "email_link", ip = command.Ip, ua = command.UserAgent }, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new EmailActionResultDto(approval.Status.ToSnakeCase(), approval.EntityType.ToSnakeCase()));
    }

    private static Result<EmailActionResultDto> Fail(Error error) => Result.Failure<EmailActionResultDto>(error);
}
