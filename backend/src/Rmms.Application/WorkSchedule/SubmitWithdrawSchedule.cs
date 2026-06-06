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

    public SubmitScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
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

        await _db.SaveChangesAsync(ct);
        return Result.Success();
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
