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
/// Close an open attendance with a check-out (M05, BR-206 — Face mandatory at check-out too).
/// A fake-GPS check-out is blocked; a geofence/face anomaly at check-out escalates an otherwise
/// valid record to the Admin review queue (handled by <see cref="AttendanceRecord.CheckOut"/>).
/// </summary>
public sealed record CheckOutCommand(
    Guid UserId,
    Guid AttendanceId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    bool FakeGpsDetected,
    PhotoUpload? Selfie,
    PhotoUpload? StorePhoto,
    string? Note) : IRequest<Result<AttendanceDto>>;

public sealed class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

internal sealed class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, Result<AttendanceDto>>
{
    private readonly IAppDbContext _db;
    private readonly IFaceVerificationService _face;
    private readonly IAttendancePhotoStorage _photos;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public CheckOutCommandHandler(
        IAppDbContext db,
        IFaceVerificationService face,
        IAttendancePhotoStorage photos,
        IAuditLogger audit,
        IDateTimeProvider clock)
    {
        _db = db;
        _face = face;
        _photos = photos;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<AttendanceDto>> Handle(CheckOutCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.Id == command.AttendanceId && a.UserId == command.UserId, ct);
        if (record is null)
        {
            return Result.Failure<AttendanceDto>(Error.NotFound(ErrorCodes.AttendanceNotFound, "Không tìm thấy lượt chấm công."));
        }
        if (!record.IsOpen)
        {
            return Result.Failure<AttendanceDto>(
                Error.Conflict(ErrorCodes.NoOpenAttendance, "Lượt chấm công này không ở trạng thái có thể check-out."));
        }

        // BR-205 — fake GPS blocks the check-out; the attendance stays open.
        if (command.FakeGpsDetected)
        {
            await _audit.RecordAsync(
                AuditAction.AttendanceFakeGpsBlocked, "attendance", record.Id,
                new { record.UserId, phase = "check_out" }, ct);
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AttendanceDto>(
                Error.Validation(ErrorCodes.FakeGpsDetected, "Phát hiện GPS giả lập — không thể check-out."));
        }

        var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == record.StoreId, ct);
        if (store is null)
        {
            return Result.Failure<AttendanceDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm bán."));
        }

        var distanceMeters = CheckInCommandHandler.DistanceMeters(
            command.Latitude, command.Longitude, store.Latitude, store.Longitude);
        var face = await _face.VerifyAsync(command.UserId, command.Selfie, ct);
        var selfieUrl = command.Selfie is null ? null : await _photos.SaveAsync(command.UserId, "check_out_selfie", command.Selfie, ct);
        var storePhotoUrl = command.StorePhoto is null ? null : await _photos.SaveAsync(command.UserId, "check_out_store_photo", command.StorePhoto, ct);

        record.CheckOut(new AttendanceCheckOutData(
            now, (decimal)command.Latitude, (decimal)command.Longitude, distanceMeters,
            face.Result, face.Confidence, selfieUrl, storePhotoUrl, command.Note));

        await _audit.RecordAsync(
            AuditAction.AttendanceCheckedOut, "attendance", record.Id,
            new { record.UserId, record.StoreId, status = record.Status.ToString() }, ct);

        await _db.SaveChangesAsync(ct);
        var dto = await AttendanceQueries.PresignAsync(_photos, AttendanceMapper.ToDto(record, store.Code, store.Name), ct);
        return Result.Success(dto);
    }
}
