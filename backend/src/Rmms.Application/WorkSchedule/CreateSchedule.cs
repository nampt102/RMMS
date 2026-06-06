using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Scheduling;
using Rmms.Shared.Errors;

namespace Rmms.Application.Scheduling;

/// <summary>
/// Register a work schedule for one or more days (M07, BR-301 day/week/month — the client
/// expands a range into explicit days). All-or-nothing: one transaction (BR bulk note).
/// Identity (<paramref name="UserId"/>) comes from the JWT, set by the controller.
/// </summary>
public sealed record CreateScheduleCommand(Guid UserId, IReadOnlyList<ScheduleDayRequest> Days)
    : IRequest<Result<IReadOnlyList<Guid>>>;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.Days).NotEmpty().WithErrorCode("REQUIRED");
        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Shifts).NotEmpty().WithErrorCode("REQUIRED");
        });
    }
}

internal sealed class CreateScheduleCommandHandler
    : IRequestHandler<CreateScheduleCommand, Result<IReadOnlyList<Guid>>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public CreateScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(CreateScheduleCommand command, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        // BR-306: schedules are registered for today/future only — never the past.
        if (command.Days.Any(d => d.Date < today))
        {
            return Result.Failure<IReadOnlyList<Guid>>(
                Error.Validation(ErrorCodes.ScheduleDateInPast, "Không thể đăng ký lịch cho ngày trong quá khứ."));
        }

        // Every shift's store must be in the user's active store assignments (BR-303).
        var requestedStoreIds = command.Days.SelectMany(d => d.Shifts).Select(s => s.StoreId).Distinct().ToList();
        var assignedStoreIds = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == command.UserId && a.EffectiveTo == null && requestedStoreIds.Contains(a.StoreId))
            .Select(a => a.StoreId)
            .ToListAsync(ct);
        if (requestedStoreIds.Except(assignedStoreIds).Any())
        {
            return Result.Failure<IReadOnlyList<Guid>>(
                Error.Validation(ErrorCodes.StoreNotAssigned, "Có điểm bán chưa được phân công cho người dùng."));
        }

        // Reject days that already have a live schedule (pending/approved/edit_pending).
        var dates = command.Days.Select(d => d.Date).ToList();
        var liveStatuses = new[] { WorkScheduleStatus.Pending, WorkScheduleStatus.Approved, WorkScheduleStatus.EditPending };
        var clashing = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.UserId == command.UserId && dates.Contains(s.ScheduleDate) && liveStatuses.Contains(s.Status))
            .Select(s => s.ScheduleDate)
            .ToListAsync(ct);
        if (clashing.Count > 0)
        {
            return Result.Failure<IReadOnlyList<Guid>>(
                Error.Conflict(ErrorCodes.Conflict, "Đã tồn tại lịch cho một hoặc nhiều ngày được chọn."));
        }

        var created = new List<Guid>(command.Days.Count);
        try
        {
            foreach (var day in command.Days)
            {
                var inputs = day.Shifts.Select(s => new ScheduleShiftInput(s.StoreId, s.StartTime, s.EndTime)).ToList();
                var schedule = Domain.Scheduling.WorkSchedule.Create(command.UserId, day.Date, inputs);
                _db.WorkSchedules.Add(schedule);
                created.Add(schedule.Id);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Domain invariant (e.g. overlapping shift windows) — surface as a clean validation error.
            return Result.Failure<IReadOnlyList<Guid>>(Error.Validation(ErrorCodes.ShiftOverlap, ex.Message));
        }

        await _audit.RecordAsync(
            AuditAction.ScheduleCreated, "work_schedule", command.UserId,
            new { user_id = command.UserId, days = command.Days.Count, schedule_ids = created }, ct);

        await _db.SaveChangesAsync(ct);
        IReadOnlyList<Guid> result = created;
        return Result.Success(result);
    }
}
