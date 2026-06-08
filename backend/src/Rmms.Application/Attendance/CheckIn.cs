using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Attendance;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.ValueObjects;
using Rmms.Shared.Errors;

namespace Rmms.Application.Attendance;

/// <summary>
/// Submit a check-in (M05, BR-201..BR-210 / AC-4/5/6/9/10). Validation order mirrors the M05
/// spec: no fake GPS (BR-205) → store assigned (BR-201) → shift in the allowed window
/// (BR-202/203) → GPS geofence (BR-204) → Face Verification (BR-206). The resulting status is
/// decided by the domain (<see cref="AttendanceRecord.CheckIn"/>). Identity comes from the JWT.
/// </summary>
public sealed record CheckInCommand(
    Guid UserId,
    Guid StoreId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    bool FakeGpsDetected,
    PhotoUpload? Selfie,
    PhotoUpload? StorePhoto,
    string? Note) : IRequest<Result<AttendanceDto>>;

public sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

internal sealed class CheckInCommandHandler : IRequestHandler<CheckInCommand, Result<AttendanceDto>>
{
    private readonly IAppDbContext _db;
    private readonly IFaceVerificationService _face;
    private readonly IAttendancePhotoStorage _photos;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifier;

    public CheckInCommandHandler(
        IAppDbContext db,
        IFaceVerificationService face,
        IAttendancePhotoStorage photos,
        IAuditLogger audit,
        IDateTimeProvider clock,
        INotificationService notifier)
    {
        _db = db;
        _face = face;
        _photos = photos;
        _audit = audit;
        _clock = clock;
        _notifier = notifier;
    }

    public async ValueTask<Result<AttendanceDto>> Handle(CheckInCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var vnToday = AttendanceTime.VnToday(now);

        // (1) BR-205 — fake/mock GPS is blocked outright: no valid record is created, audit only.
        if (command.FakeGpsDetected)
        {
            await _audit.RecordAsync(
                AuditAction.AttendanceFakeGpsBlocked, "attendance", command.UserId,
                new { command.UserId, command.StoreId, command.Latitude, command.Longitude }, ct);
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AttendanceDto>(
                Error.Validation(ErrorCodes.FakeGpsDetected, "Phát hiện GPS giả lập — không thể chấm công."));
        }

        // (2) Block a second concurrent open attendance (forgot to check out → close it first).
        var hasOpen = await _db.AttendanceRecords.AsNoTracking().AnyAsync(
            a => a.UserId == command.UserId && a.CheckOutAt == null
                 && AttendanceTime.OpenStatuses.Contains(a.Status), ct);
        if (hasOpen)
        {
            return Result.Failure<AttendanceDto>(
                Error.Conflict(ErrorCodes.AlreadyCheckedIn, "Bạn đang có ca chấm công chưa check-out."));
        }

        // (3) BR-201 — the store must be in the user's active store assignments.
        var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == command.StoreId, ct);
        if (store is null)
        {
            return Result.Failure<AttendanceDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm bán."));
        }
        var assigned = await _db.UserStoreAssignments.AsNoTracking().AnyAsync(
            a => a.UserId == command.UserId && a.StoreId == command.StoreId
                 && a.EffectiveFrom <= vnToday && (a.EffectiveTo == null || a.EffectiveTo >= vnToday), ct);
        if (!assigned)
        {
            return Result.Failure<AttendanceDto>(
                Error.Validation(ErrorCodes.StoreNotAssigned, "Điểm bán này chưa được phân công cho bạn."));
        }

        // (4) BR-202/203 — resolve today's approved shift at this store inside the allowed window.
        var resolution = await ResolveShiftAsync(command.UserId, command.StoreId, vnToday, now, ct);
        if (resolution.Error is { } shiftError)
        {
            return Result.Failure<AttendanceDto>(shiftError);
        }
        var (shiftId, isLate) = (resolution.ShiftId, resolution.IsLate);

        // Each scheduled shift maps to at most one (non-rejected) attendance record.
        var shiftUsed = await _db.AttendanceRecords.AsNoTracking().AnyAsync(
            a => a.WorkScheduleShiftId == shiftId && a.Status != AttendanceStatus.AdminRejected, ct);
        if (shiftUsed)
        {
            return Result.Failure<AttendanceDto>(
                Error.Conflict(ErrorCodes.AlreadyCheckedIn, "Ca này đã được chấm công."));
        }

        // (5) BR-204 — Haversine geofence distance.
        var distanceMeters = DistanceMeters(command.Latitude, command.Longitude, store.Latitude, store.Longitude);

        // (6) BR-206 — Face Verification (stub passes in Phase 1A; real provider at M06).
        var face = await _face.VerifyAsync(command.UserId, command.Selfie, ct);

        // Upload photos (stub returns a placeholder URL until M13/MinIO).
        var selfieUrl = await SavePhotoAsync(command.UserId, "check_in_selfie", command.Selfie, ct);
        var storePhotoUrl = await SavePhotoAsync(command.UserId, "check_in_store_photo", command.StorePhoto, ct);

        var record = AttendanceRecord.CheckIn(new AttendanceCheckInData(
            command.UserId, shiftId, command.StoreId, now,
            (decimal)command.Latitude, (decimal)command.Longitude, distanceMeters,
            FakeGpsDetected: false, face.Result, face.Confidence,
            selfieUrl, storePhotoUrl, isLate, command.Note));

        _db.AttendanceRecords.Add(record);

        await _audit.RecordAsync(
            AuditAction.AttendanceCheckedIn, "attendance", record.Id,
            new { record.UserId, record.StoreId, record.WorkScheduleShiftId, status = record.Status.ToString(), record.IsLate }, ct);
        if (record.Status == AttendanceStatus.GpsViolationPendingReview)
        {
            await _audit.RecordAsync(AuditAction.AttendanceGpsViolation, "attendance", record.Id,
                new { record.CheckInDistanceMeters }, ct);
        }
        if (record.Status == AttendanceStatus.FaceFailPendingReview)
        {
            await _audit.RecordAsync(AuditAction.AttendanceFaceFailed, "attendance", record.Id,
                new { face.Confidence }, ct);
        }

        // CR-2: tell the PG their check-in is awaiting Admin Review (in-app only, CR-3).
        if (record.RequiresReview)
        {
            await _notifier.NotifyAsync(record.UserId, AttendanceNotifications.InReview(record), ct);
        }

        await _db.SaveChangesAsync(ct);
        var dto = await AttendanceQueries.PresignAsync(_photos, AttendanceMapper.ToDto(record, store.Code, store.Name), ct);
        return Result.Success(dto);
    }

    private async Task<ShiftResolution> ResolveShiftAsync(
        Guid userId, Guid storeId, DateOnly vnToday, DateTimeOffset now, CancellationToken ct)
    {
        // Approved schedules for the day; owned shifts load automatically.
        var schedules = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.UserId == userId && s.ScheduleDate == vnToday && s.Status == WorkScheduleStatus.Approved)
            .ToListAsync(ct);

        var atStore = schedules
            .SelectMany(s => s.Shifts)
            .Where(sh => sh.StoreId == storeId)
            .ToList();
        if (atStore.Count == 0)
        {
            return ShiftResolution.Fail(
                Error.NotFound(ErrorCodes.ShiftNotFound, "Không có ca làm đã duyệt cho điểm bán này hôm nay."));
        }

        var earlyWindow = TimeSpan.FromMinutes(AttendanceTime.EarlyCheckInMinutes);
        var lateThreshold = TimeSpan.FromMinutes(AttendanceTime.LateThresholdMinutes);

        // Earliest shift whose window already opened (and not yet ended).
        var candidates = atStore
            .Select(sh => new
            {
                sh.Id,
                Start = AttendanceTime.ToUtc(vnToday, sh.StartTime),
                End = AttendanceTime.ToUtc(vnToday, sh.EndTime),
            })
            .OrderBy(x => x.Start)
            .ToList();

        var open = candidates.FirstOrDefault(x => now >= x.Start - earlyWindow && now <= x.End);
        if (open is null)
        {
            // Distinguish "too early" (window not open yet) from "no usable shift".
            var anyUpcoming = candidates.Any(x => now < x.Start - earlyWindow);
            return ShiftResolution.Fail(anyUpcoming
                ? Error.Validation(ErrorCodes.CheckInTooEarly, "Chưa đến giờ được phép check-in (sớm tối đa 60 phút).")
                : Error.Validation(ErrorCodes.ShiftNotFound, "Không có ca làm phù hợp để chấm công lúc này."));
        }

        var isLate = now > open.Start + lateThreshold;
        return ShiftResolution.Ok(open.Id, isLate);
    }

    private async Task<string?> SavePhotoAsync(Guid userId, string kind, PhotoUpload? photo, CancellationToken ct) =>
        photo is null ? null : await _photos.SaveAsync(userId, kind, photo, ct);

    internal static decimal DistanceMeters(double lat, double lng, decimal storeLat, decimal storeLng)
    {
        var from = new GpsCoordinate(lat, lng);
        var to = new GpsCoordinate((double)storeLat, (double)storeLng);
        return Math.Round((decimal)from.DistanceMetersTo(to), 2);
    }

    private readonly record struct ShiftResolution(Guid ShiftId, bool IsLate, Error? Error)
    {
        public static ShiftResolution Ok(Guid shiftId, bool isLate) => new(shiftId, isLate, null);
        public static ShiftResolution Fail(Error error) => new(Guid.Empty, false, error);
    }
}
