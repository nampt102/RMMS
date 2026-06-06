using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Scheduling;

// ===== Submit for approval (BR-307) =====

public sealed record SubmitScheduleCommand(Guid ScheduleId, Guid UserId) : IRequest<Result>;

internal sealed class SubmitScheduleCommandHandler : IRequestHandler<SubmitScheduleCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;
    private readonly IApprovalService _approvals;

    public SubmitScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock, IApprovalService approvals)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
        _approvals = approvals;
    }

    public async ValueTask<Result> Handle(SubmitScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _db.WorkSchedules
            .SingleOrDefaultAsync(s => s.Id == command.ScheduleId && s.UserId == command.UserId, ct);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.ScheduleNotFound, "Không tìm thấy lịch làm việc."));
        }

        if (schedule.Status != WorkScheduleStatus.Pending)
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ScheduleNotPending, "Chỉ lịch ở trạng thái chờ mới có thể gửi duyệt."));
        }

        schedule.Submit(_clock.UtcNow);

        await _audit.RecordAsync(
            AuditAction.ScheduleSubmitted, "work_schedule", schedule.Id,
            new { user_id = schedule.UserId, schedule_date = schedule.ScheduleDate }, ct);

        // M09: route to the PG's active Leader (BR-405). Leader→BUH (BR-406) is skipped
        // until a Leader↔BUH assignment exists. Idempotent — no duplicate pending row.
        await CreateApprovalIfRoutableAsync(schedule.Id, schedule.UserId, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task CreateApprovalIfRoutableAsync(Guid scheduleId, Guid ownerId, CancellationToken ct)
    {
        var alreadyQueued = await _db.Approvals.AnyAsync(
            a => a.EntityType == ApprovalEntityType.WorkSchedule
              && a.EntityId == scheduleId
              && a.Status == ApprovalStatus.Pending, ct);
        if (alreadyQueued) return;

        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, ct);
        if (owner is null || owner.Role != UserRole.Pg) return; // only PG→Leader wired in Phase 1

        var leaderId = await _db.UserLeaderAssignments
            .Where(a => a.PgUserId == ownerId && a.EffectiveTo == null)
            .Select(a => a.LeaderUserId)
            .FirstOrDefaultAsync(ct);
        if (leaderId == Guid.Empty) return; // no active leader → nothing to route to

        await _approvals.CreateAsync(
            ApprovalEntityType.WorkSchedule, scheduleId, ownerId, leaderId, UserRole.Leader, ct);
    }
}

// ===== Withdraw a pending / edit-pending schedule =====

public sealed record WithdrawScheduleCommand(Guid ScheduleId, Guid UserId) : IRequest<Result>;

internal sealed class WithdrawScheduleCommandHandler : IRequestHandler<WithdrawScheduleCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public WithdrawScheduleCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(WithdrawScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _db.WorkSchedules
            .SingleOrDefaultAsync(s => s.Id == command.ScheduleId && s.UserId == command.UserId, ct);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.ScheduleNotFound, "Không tìm thấy lịch làm việc."));
        }

        if (schedule.Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending))
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ScheduleNotEditable, "Chỉ lịch đang chờ duyệt mới có thể thu hồi."));
        }

        // Soft-delete (ADR-004). If this was an edit_pending, the old approved version stays effective.
        _db.WorkSchedules.Remove(schedule);

        await _audit.RecordAsync(
            AuditAction.ScheduleWithdrawn, "work_schedule", schedule.Id,
            new { user_id = schedule.UserId, schedule_date = schedule.ScheduleDate }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
