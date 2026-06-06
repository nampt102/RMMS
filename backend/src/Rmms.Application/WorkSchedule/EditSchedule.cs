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
/// Edit a schedule's shifts (M07, BR-306/BR-307/BR-308). Editing a still-pending row updates
/// it in place; editing an APPROVED row creates a new edit_pending version (the old stays
/// effective until the edit is approved — BR-308). Returns the id of the row to track.
/// </summary>
public sealed record EditScheduleCommand(Guid ScheduleId, Guid UserId, IReadOnlyList<ScheduleShiftRequest> Shifts)
    : IRequest<Result<Guid>>;

internal sealed class EditScheduleCommandHandler : IRequestHandler<EditScheduleCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public EditScheduleCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<Guid>> Handle(EditScheduleCommand command, CancellationToken ct)
    {
        if (command.Shifts is null || command.Shifts.Count == 0)
        {
            return Result.Failure<Guid>(Error.Validation("REQUIRED", "Lịch phải có ít nhất một ca."));
        }

        // Shifts are an owned collection → loaded automatically with the schedule.
        var schedule = await _db.WorkSchedules
            .SingleOrDefaultAsync(s => s.Id == command.ScheduleId && s.UserId == command.UserId, ct);
        if (schedule is null)
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.ScheduleNotFound, "Không tìm thấy lịch làm việc."));
        }

        // BR-306: only future (or today) dates are editable.
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        if (schedule.ScheduleDate < today)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ScheduleDateInPast, "Không thể sửa lịch của ngày đã qua."));
        }

        // New shifts' stores must be in the user's active store assignments (BR-303).
        var storeIds = command.Shifts.Select(s => s.StoreId).Distinct().ToList();
        var assigned = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == command.UserId && a.EffectiveTo == null && storeIds.Contains(a.StoreId))
            .Select(a => a.StoreId)
            .ToListAsync(ct);
        if (storeIds.Except(assigned).Any())
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.StoreNotAssigned, "Có điểm bán chưa được phân công cho người dùng."));
        }

        var inputs = command.Shifts.Select(s => new ScheduleShiftInput(s.StoreId, s.StartTime, s.EndTime)).ToList();

        Guid trackedId;
        try
        {
            switch (schedule.Status)
            {
                case WorkScheduleStatus.Pending:
                case WorkScheduleStatus.EditPending:
                    // Owned-collection replacement: EF deletes the old shift rows and inserts the
                    // new ones wholesale on SaveChanges.
                    schedule.ReplaceShifts(inputs);
                    trackedId = schedule.Id;
                    break;

                case WorkScheduleStatus.Approved:
                    var edit = schedule.CreateEditedVersion(inputs, _clock.UtcNow);
                    _db.WorkSchedules.Add(edit);
                    trackedId = edit.Id;
                    break;

                default:
                    return Result.Failure<Guid>(
                        Error.Conflict(ErrorCodes.ScheduleNotEditable, "Lịch ở trạng thái này không thể sửa."));
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ShiftOverlap, ex.Message));
        }

        await _audit.RecordAsync(
            AuditAction.ScheduleEdited, "work_schedule", trackedId,
            new { user_id = schedule.UserId, schedule_date = schedule.ScheduleDate, original_id = schedule.Id }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success(trackedId);
    }
}
