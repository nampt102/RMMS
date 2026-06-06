using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Scheduling;

// ===== Approve (BR-405/BR-406 routing; Leader for PG, Admin any) =====

public sealed record ApproveScheduleCommand(Guid ScheduleId, Guid ApproverUserId, bool ApproverIsAdmin) : IRequest<Result>;

internal sealed class ApproveScheduleCommandHandler : IRequestHandler<ApproveScheduleCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ApproveScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ApproveScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _db.WorkSchedules.SingleOrDefaultAsync(s => s.Id == command.ScheduleId, ct);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.ScheduleNotFound, "Không tìm thấy lịch làm việc."));
        }

        if (schedule.Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ApprovalNotPending, "Lịch không ở trạng thái chờ duyệt."));
        }

        var scope = await EnsureApproverScopeAsync(_db, command.ApproverUserId, command.ApproverIsAdmin, schedule.UserId, ct);
        if (scope is not null) return scope;

        // BR-308: approving an edit supersedes the still-effective old approved version.
        if (schedule.Status == WorkScheduleStatus.EditPending && schedule.PreviousVersionId is { } prevId)
        {
            var previous = await _db.WorkSchedules
                .SingleOrDefaultAsync(s => s.Id == prevId && s.Status == WorkScheduleStatus.Approved, ct);
            previous?.Supersede();
        }

        schedule.Approve(command.ApproverUserId, _clock.UtcNow);

        await _audit.RecordAsync(
            AuditAction.ScheduleApproved, "work_schedule", schedule.Id,
            new { schedule.UserId, schedule.ScheduleDate, approver = command.ApproverUserId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    internal static async Task<Result?> EnsureApproverScopeAsync(
        IAppDbContext db, Guid approverUserId, bool approverIsAdmin, Guid targetUserId, CancellationToken ct)
    {
        if (approverIsAdmin) return null;

        var manages = await db.UserLeaderAssignments.AnyAsync(
            a => a.LeaderUserId == approverUserId && a.PgUserId == targetUserId && a.EffectiveTo == null, ct);
        return manages
            ? null
            : Result.Failure(Error.Forbidden(ErrorCodes.NotApprover, "Bạn không quản lý người dùng này nên không thể duyệt lịch."));
    }
}

// ===== Reject (BR-404: reason required) =====

public sealed record RejectScheduleCommand(Guid ScheduleId, Guid ApproverUserId, bool ApproverIsAdmin, string Reason)
    : IRequest<Result>;

public sealed class RejectScheduleCommandValidator : AbstractValidator<RejectScheduleCommand>
{
    public RejectScheduleCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode(ErrorCodes.RejectReasonRequired).MaximumLength(1000);
    }
}

internal sealed class RejectScheduleCommandHandler : IRequestHandler<RejectScheduleCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public RejectScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(RejectScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _db.WorkSchedules.SingleOrDefaultAsync(s => s.Id == command.ScheduleId, ct);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.ScheduleNotFound, "Không tìm thấy lịch làm việc."));
        }

        if (schedule.Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ApprovalNotPending, "Lịch không ở trạng thái chờ duyệt."));
        }

        var scope = await ApproveScheduleCommandHandler.EnsureApproverScopeAsync(
            _db, command.ApproverUserId, command.ApproverIsAdmin, schedule.UserId, ct);
        if (scope is not null) return scope;

        // BR-308: rejecting an edit leaves the old approved version effective (unchanged).
        schedule.Reject(command.ApproverUserId, command.Reason, _clock.UtcNow);

        await _audit.RecordAsync(
            AuditAction.ScheduleRejected, "work_schedule", schedule.Id,
            new { schedule.UserId, schedule.ScheduleDate, approver = command.ApproverUserId, reason = command.Reason }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
