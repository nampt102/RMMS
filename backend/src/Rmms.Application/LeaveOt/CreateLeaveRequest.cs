using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Shared.Errors;

namespace Rmms.Application.LeaveOt;

// ===== Regular leave =====

public sealed record CreateLeaveRequestCommand(
    Guid UserId, DateOnly StartDate, DateOnly EndDate, TimeOnly? StartTime, TimeOnly? EndTime, string Reason)
    : IRequest<Result<LeaveRequestDto>>;

public sealed class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(1000);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<LeaveRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalService _approvals;
    private readonly IAuditLogger _audit;

    public CreateLeaveRequestCommandHandler(IAppDbContext db, IApprovalService approvals, IAuditLogger audit)
    {
        _db = db;
        _approvals = approvals;
        _audit = audit;
    }

    public async ValueTask<Result<LeaveRequestDto>> Handle(CreateLeaveRequestCommand command, CancellationToken ct)
    {
        var request = LeaveRequest.Create(
            command.UserId, LeaveType.Regular, command.StartDate, command.EndDate,
            command.StartTime, command.EndTime, command.Reason);
        _db.LeaveRequests.Add(request);

        await LeaveOtProducer.RouteAsync(_db, _approvals, ApprovalEntityType.LeaveRequest, request.Id, command.UserId,
            id => request.LinkApproval(id), ct);

        await _audit.RecordAsync(AuditAction.LeaveRequested, "leave_request", request.Id,
            new { request.UserId, type = "regular", request.StartDate, request.EndDate }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(LeaveOtMapper.ToDto(request));
    }
}

// ===== Emergency leave (tied to the open check-in) =====

public sealed record CreateEmergencyLeaveCommand(Guid UserId, string Reason) : IRequest<Result<LeaveRequestDto>>;

internal sealed class CreateEmergencyLeaveCommandHandler : IRequestHandler<CreateEmergencyLeaveCommand, Result<LeaveRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalService _approvals;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public CreateEmergencyLeaveCommandHandler(IAppDbContext db, IApprovalService approvals, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _approvals = approvals;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<LeaveRequestDto>> Handle(CreateEmergencyLeaveCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure<LeaveRequestDto>(Error.Validation(ErrorCodes.ValidationFailed, "Vui lòng nhập lý do."));

        // Emergency leave is only valid while there is an open check-in (BR / edge case).
        var openAttendance = await _db.AttendanceRecords
            .Where(a => a.UserId == command.UserId && a.CheckOutAt == null)
            .OrderByDescending(a => a.CheckInAt)
            .FirstOrDefaultAsync(ct);
        if (openAttendance is null)
            return Result.Failure<LeaveRequestDto>(Error.Conflict(ErrorCodes.NoOpenAttendance, "Không có ca đang mở để xin nghỉ đột xuất."));

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime.AddHours(7)); // VN local (CR-5)
        var request = LeaveRequest.Create(
            command.UserId, LeaveType.Emergency, today, today, null, null, command.Reason, openAttendance.Id);
        _db.LeaveRequests.Add(request);

        await LeaveOtProducer.RouteAsync(_db, _approvals, ApprovalEntityType.LeaveRequest, request.Id, command.UserId,
            id => request.LinkApproval(id), ct);

        await _audit.RecordAsync(AuditAction.LeaveRequested, "leave_request", request.Id,
            new { request.UserId, type = "emergency", attendance = openAttendance.Id }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(LeaveOtMapper.ToDto(request));
    }
}
