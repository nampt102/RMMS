using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Attendance;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Attendance;

/// <summary>
/// Admin resolves a pending-review attendance (BR-208 approve / BR-209 reject). Reject requires
/// a reason (BR-404). Only records in the review queue (<see cref="AttendanceRecord.RequiresReview"/>)
/// are actionable.
/// </summary>
public sealed record ReviewAttendanceCommand(Guid AttendanceId, Guid ReviewerUserId, bool Approve, string? Note)
    : IRequest<Result>;

public sealed class ReviewAttendanceCommandValidator : AbstractValidator<ReviewAttendanceCommand>
{
    public ReviewAttendanceCommandValidator()
    {
        // BR-404: a rejection must carry a reason.
        RuleFor(x => x.Note)
            .NotEmpty().WithErrorCode(ErrorCodes.RejectReasonRequired)
            .MaximumLength(1000)
            .When(x => !x.Approve);
    }
}

internal sealed class ReviewAttendanceCommandHandler : IRequestHandler<ReviewAttendanceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ReviewAttendanceCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ReviewAttendanceCommand command, CancellationToken ct)
    {
        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == command.AttendanceId, ct);
        if (record is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.AttendanceNotFound, "Không tìm thấy lượt chấm công."));
        }
        if (!record.RequiresReview)
        {
            return Result.Failure(
                Error.Conflict(ErrorCodes.AttendanceNotReviewable, "Lượt chấm công này không ở trạng thái chờ duyệt."));
        }

        if (command.Approve)
        {
            record.ApproveReview(command.ReviewerUserId, command.Note, _clock.UtcNow);
        }
        else
        {
            record.RejectReview(command.ReviewerUserId, command.Note!, _clock.UtcNow);
        }

        await _audit.RecordAsync(
            AuditAction.AttendanceReviewed, "attendance", record.Id,
            new { reviewer = command.ReviewerUserId, decision = command.Approve ? "approved" : "rejected", note = command.Note }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
