using FluentAssertions;
using Rmms.Application.Attendance;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Attendance;

/// <summary>
/// M05 attendance handler tests. The <see cref="TestClock"/> default is 2026-06-01 09:00Z, i.e.
/// 16:00 VN — so shift windows below are written in VN-local time and chosen relative to that.
/// </summary>
public sealed class AttendanceHandlerTests
{
    // ---- fixtures ----

    private static User Pg(string email = "pg@example.com") => UserFactory.CreateActivePg(email);

    private static async Task<(User Pg, Store Store)> SeedAsync(
        AppDbContext db, TestClock clock, decimal storeLat = 1m, decimal storeLng = 1m)
    {
        var pg = Pg();
        var store = Store.Create("ST-1", "Store 1", "Addr", storeLat, storeLng, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        db.UserStoreAssignments.Add(
            UserStoreAssignment.Create(pg.Id, store.Id, AttendanceTime.VnToday(clock.UtcNow)));
        await db.SaveChangesAsync();
        return (pg, store);
    }

    /// <summary>Seed an APPROVED schedule with one shift at the store; returns the shift id.</summary>
    private static async Task<Guid> SeedApprovedShiftAsync(
        AppDbContext db, TestClock clock, Guid pgId, Guid storeId, TimeOnly startVn, TimeOnly endVn)
    {
        var date = AttendanceTime.VnToday(clock.UtcNow);
        var schedule = Rmms.Domain.Scheduling.WorkSchedule.Create(
            pgId, date, new[] { new Rmms.Domain.Scheduling.ScheduleShiftInput(storeId, startVn, endVn) });
        schedule.Approve(Guid.NewGuid(), clock.UtcNow);
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule.Shifts.Single().Id;
    }

    private static CheckInCommandHandler CheckInHandler(
        AppDbContext db, TestClock clock, InMemoryAuditLogger audit,
        FaceVerificationResult face = FaceVerificationResult.Success) =>
        new(db, new FakeFaceVerificationService(face), new FakePhotoStorage(), audit, clock);

    private static CheckInCommand Cmd(Guid userId, Guid storeId, double lat = 1, double lng = 1, bool fakeGps = false) =>
        new(userId, storeId, lat, lng, 10, fakeGps, null, null, null);

    // On-time window: shift 16:00–18:00 VN; now is 16:00 VN exactly → not late.
    private static readonly TimeOnly OnTimeStart = new(16, 0);
    private static readonly TimeOnly OnTimeEnd = new(18, 0);

    // ---- check-in ----

    [Fact]
    public async Task CheckIn_OnTime_Valid_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);

        var result = await CheckInHandler(db, clock, audit).Handle(Cmd(pg.Id, store.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AttendanceStatus.Valid.ToString().ToLowerInvariant());
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.Valid);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceCheckedIn);
    }

    [Fact]
    public async Task CheckIn_AfterThreshold_Late()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        // Start 15:00 VN (08:00Z); now 09:00Z is >5 min after → Late.
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, new TimeOnly(15, 0), new TimeOnly(18, 0));

        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.Late);
        db.AttendanceRecords.Single().IsLate.Should().BeTrue();
    }

    [Fact]
    public async Task CheckIn_BeforeEarlyWindow_TooEarly()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        // Start 18:00 VN (11:00Z); early window opens 10:00Z; now 09:00Z → too early.
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, new TimeOnly(18, 0), new TimeOnly(20, 0));

        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CheckInTooEarly);
    }

    [Fact]
    public async Task CheckIn_StoreNotAssigned_Fails()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = Pg();
        var store = Store.Create("ST-9", "Unassigned", null, 1m, 1m, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.StoreNotAssigned);
    }

    [Fact]
    public async Task CheckIn_NoApprovedShift_ShiftNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        // No schedule seeded.

        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ShiftNotFound);
    }

    [Fact]
    public async Task CheckIn_GpsBeyond300m_GpsViolation_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock, storeLat: 1m, storeLng: 1m);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);

        // Check in ~157 km away.
        var result = await CheckInHandler(db, clock, audit).Handle(Cmd(pg.Id, store.Id, lat: 2, lng: 2), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.GpsViolationPendingReview);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceGpsViolation);
    }

    [Fact]
    public async Task CheckIn_FaceFail_FaceFailPendingReview_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);

        var result = await CheckInHandler(db, clock, audit, FaceVerificationResult.Fail).Handle(Cmd(pg.Id, store.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.FaceFailPendingReview);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceFaceFailed);
    }

    [Fact]
    public async Task CheckIn_FakeGps_Blocked_NoRecord_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);

        var result = await CheckInHandler(db, clock, audit).Handle(Cmd(pg.Id, store.Id, fakeGps: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.FakeGpsDetected);
        db.AttendanceRecords.Should().BeEmpty();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceFakeGpsBlocked);
    }

    [Fact]
    public async Task CheckIn_WhenAlreadyOpen_AlreadyCheckedIn()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);

        (await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default))
            .IsSuccess.Should().BeTrue();

        // A second check-in (even a fresh shift would be blocked) while one is open.
        var second = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pg.Id, store.Id), default);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(ErrorCodes.AlreadyCheckedIn);
    }

    // ---- check-out ----

    private static CheckOutCommandHandler CheckOutHandler(
        AppDbContext db, TestClock clock, InMemoryAuditLogger audit,
        FaceVerificationResult face = FaceVerificationResult.Success) =>
        new(db, new FakeFaceVerificationService(face), new FakePhotoStorage(), audit, clock);

    private static async Task<Guid> CheckInOnceAsync(AppDbContext db, TestClock clock, Guid pgId, Guid storeId)
    {
        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger()).Handle(Cmd(pgId, storeId), default);
        return result.Value.Id;
    }

    [Fact]
    public async Task CheckOut_Open_Closes_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        var id = await CheckInOnceAsync(db, clock, pg.Id, store.Id);

        clock.Advance(TimeSpan.FromHours(1));
        var result = await CheckOutHandler(db, clock, audit)
            .Handle(new CheckOutCommand(pg.Id, id, 1, 1, 10, false, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        var rec = db.AttendanceRecords.Single();
        rec.CheckOutAt.Should().NotBeNull();
        rec.Status.Should().Be(AttendanceStatus.Valid);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceCheckedOut);
    }

    [Fact]
    public async Task CheckOut_NotFound_Fails()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, _) = await SeedAsync(db, clock);

        var result = await CheckOutHandler(db, clock, new InMemoryAuditLogger())
            .Handle(new CheckOutCommand(pg.Id, Guid.NewGuid(), 1, 1, 10, false, null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AttendanceNotFound);
    }

    [Fact]
    public async Task CheckOut_FaceFail_EscalatesToReview()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        var id = await CheckInOnceAsync(db, clock, pg.Id, store.Id);

        var result = await CheckOutHandler(db, clock, new InMemoryAuditLogger(), FaceVerificationResult.Fail)
            .Handle(new CheckOutCommand(pg.Id, id, 1, 1, 10, false, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.FaceFailPendingReview);
    }

    [Fact]
    public async Task CheckOut_FakeGps_Blocked_StaysOpen()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        var id = await CheckInOnceAsync(db, clock, pg.Id, store.Id);

        var result = await CheckOutHandler(db, clock, new InMemoryAuditLogger())
            .Handle(new CheckOutCommand(pg.Id, id, 1, 1, 10, true, null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.FakeGpsDetected);
        db.AttendanceRecords.Single().CheckOutAt.Should().BeNull();
    }

    // ---- admin review ----

    private static async Task<Guid> SeedGpsViolationAsync(AppDbContext db, TestClock clock, Guid pgId, Guid storeId)
    {
        await SeedApprovedShiftAsync(db, clock, pgId, storeId, OnTimeStart, OnTimeEnd);
        var result = await CheckInHandler(db, clock, new InMemoryAuditLogger())
            .Handle(Cmd(pgId, storeId, lat: 2, lng: 2), default);
        return result.Value.Id;
    }

    [Fact]
    public async Task Review_Approve_GpsViolation_AdminApproved()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var (pg, store) = await SeedAsync(db, clock);
        var id = await SeedGpsViolationAsync(db, clock, pg.Id, store.Id);

        var result = await new ReviewAttendanceCommandHandler(db, audit, clock)
            .Handle(new ReviewAttendanceCommand(id, Guid.NewGuid(), Approve: true, "OK on review"), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.AdminApproved);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AttendanceReviewed);
    }

    [Fact]
    public async Task Review_Reject_AdminRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        var id = await SeedGpsViolationAsync(db, clock, pg.Id, store.Id);

        var result = await new ReviewAttendanceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ReviewAttendanceCommand(id, Guid.NewGuid(), Approve: false, "Not the right place"), default);

        result.IsSuccess.Should().BeTrue();
        db.AttendanceRecords.Single().Status.Should().Be(AttendanceStatus.AdminRejected);
    }

    [Fact]
    public async Task Review_NotPendingReview_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        var id = await CheckInOnceAsync(db, clock, pg.Id, store.Id); // status Valid → not reviewable

        var result = await new ReviewAttendanceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ReviewAttendanceCommand(id, Guid.NewGuid(), Approve: true, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AttendanceNotReviewable);
    }

    // ---- queries ----

    [Fact]
    public async Task GetToday_ReturnsShift_WithAttendanceStatus()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        var shiftId = await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        await CheckInOnceAsync(db, clock, pg.Id, store.Id);

        var result = await new GetTodayQueryHandler(db, clock).Handle(new GetTodayQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        var shift = result.Value.Single();
        shift.WorkScheduleShiftId.Should().Be(shiftId);
        shift.AttendanceStatus.Should().Be(AttendanceStatus.Valid.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task GetHistory_ReturnsCallerRecords()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedAsync(db, clock);
        await SeedApprovedShiftAsync(db, clock, pg.Id, store.Id, OnTimeStart, OnTimeEnd);
        await CheckInOnceAsync(db, clock, pg.Id, store.Id);

        var result = await new GetHistoryQueryHandler(db, new FakePhotoStorage())
            .Handle(new GetHistoryQuery(pg.Id, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().ContainSingle();
        result.Value.Meta.Total.Should().Be(1);
    }
}
