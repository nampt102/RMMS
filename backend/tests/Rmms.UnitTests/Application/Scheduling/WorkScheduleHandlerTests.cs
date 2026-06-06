using FluentAssertions;
using Rmms.Application.Scheduling;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Scheduling;

public sealed class WorkScheduleHandlerTests
{
    private static User Leader(string email = "leader@example.com") =>
        User.CreateByAdmin(email, "plain:Pwd12345", "Leader X", UserRole.Leader, null, "vi");

    private static async Task<(User Pg, Store Store)> SeedPgWithStoreAsync(
        AppDbContext db, TestClock clock, string email = "pg@example.com")
    {
        var pg = UserFactory.CreateActivePg(email);
        var store = Store.Create("ST-1", "Store 1", null, 1m, 1m, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        db.UserStoreAssignments.Add(
            UserStoreAssignment.Create(pg.Id, store.Id, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        await db.SaveChangesAsync();
        return (pg, store);
    }

    private static ScheduleDayRequest Day(DateOnly date, Guid storeId, int startHour = 8, int endHour = 12) =>
        new(date, new[] { new ScheduleShiftRequest(storeId, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)) });

    private static DateOnly Future(TestClock clock, int addDays = 5) =>
        DateOnly.FromDateTime(clock.UtcNow.UtcDateTime).AddDays(addDays);

    // ----- Create -----

    [Fact]
    public async Task Create_Succeeds_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var audit = new InMemoryAuditLogger();

        var result = await new CreateScheduleCommandHandler(db, audit, clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var schedule = db.WorkSchedules.Single();
        schedule.Status.Should().Be(WorkScheduleStatus.Pending);
        schedule.Shifts.Should().ContainSingle();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.ScheduleCreated);
    }

    [Fact]
    public async Task Create_PastDate_ReturnsScheduleDateInPast()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var past = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime).AddDays(-1);

        var result = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(past, store.Id) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ScheduleDateInPast);
    }

    [Fact]
    public async Task Create_StoreNotAssigned_ReturnsStoreNotAssigned()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, _) = await SeedPgWithStoreAsync(db, clock);

        var result = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), Guid.NewGuid()) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.StoreNotAssigned);
    }

    [Fact]
    public async Task Create_OverlappingShifts_ReturnsShiftOverlap()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var date = Future(clock);
        var day = new ScheduleDayRequest(date, new[]
        {
            new ScheduleShiftRequest(store.Id, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new ScheduleShiftRequest(store.Id, new TimeOnly(11, 0), new TimeOnly(15, 0)),
        });

        var result = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { day }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ShiftOverlap);
    }

    [Fact]
    public async Task Create_DuplicateLiveDay_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var date = Future(clock);
        await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(date, store.Id) }), default);

        var result = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(date, store.Id) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    // ----- Submit -----

    [Fact]
    public async Task Submit_SetsSubmittedAt()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var id = create.Value[0];

        var result = await new SubmitScheduleCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeApprovalService())
            .Handle(new SubmitScheduleCommand(id, pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.WorkSchedules.Single(s => s.Id == id).SubmittedAt.Should().NotBeNull();
    }

    // ----- Edit + versioning (BR-308 / AC-15) -----

    [Fact]
    public async Task Edit_PendingSchedule_UpdatesInPlace()
    {
        // Separate contexts per step mirror real per-request DbContext scoping (and avoid the
        // InMemory provider's quirk when the same context both adds and re-deletes child rows).
        var dbName = Guid.NewGuid().ToString();
        var clock = new TestClock();
        Guid id, pgId, storeId;

        await using (var db = TestDbContextFactory.Create(dbName))
        {
            var (pg, store) = await SeedPgWithStoreAsync(db, clock);
            pgId = pg.Id;
            storeId = store.Id;
            var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
                .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id, 8, 12) }), default);
            id = create.Value[0];
        }

        await using (var db = TestDbContextFactory.Create(dbName))
        {
            var newShifts = new[] { new ScheduleShiftRequest(storeId, new TimeOnly(13, 0), new TimeOnly(17, 0)) };
            var result = await new EditScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
                .Handle(new EditScheduleCommand(id, pgId, newShifts), default);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(id); // same row, edited in place
        }

        await using (var assertDb = TestDbContextFactory.Create(dbName))
        {
            var schedule = assertDb.WorkSchedules.Single(); // owned shifts auto-loaded
            schedule.Shifts.Should().ContainSingle();
            schedule.Shifts.Single().StartTime.Should().Be(new TimeOnly(13, 0));
        }
    }

    [Fact]
    public async Task Edit_ApprovedSchedule_CreatesEditVersion_OldStaysApproved()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var oldId = create.Value[0];
        await new ApproveScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveScheduleCommand(oldId, Guid.NewGuid(), true), default);

        var newShifts = new[] { new ScheduleShiftRequest(store.Id, new TimeOnly(9, 0), new TimeOnly(18, 0)) };
        var result = await new EditScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new EditScheduleCommand(oldId, pg.Id, newShifts), default);

        result.IsSuccess.Should().BeTrue();
        var newId = result.Value;
        newId.Should().NotBe(oldId);
        db.WorkSchedules.Single(s => s.Id == oldId).Status.Should().Be(WorkScheduleStatus.Approved); // BR-308: old stays effective
        var edit = db.WorkSchedules.Single(s => s.Id == newId);
        edit.Status.Should().Be(WorkScheduleStatus.EditPending);
        edit.PreviousVersionId.Should().Be(oldId);
        edit.Version.Should().Be(2);
    }

    [Fact]
    public async Task ApproveEdit_SupersedesOldApproved_NewBecomesEffective()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var oldId = create.Value[0];
        await new ApproveScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveScheduleCommand(oldId, Guid.NewGuid(), true), default);
        var edit = await new EditScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new EditScheduleCommand(oldId, pg.Id,
                new[] { new ScheduleShiftRequest(store.Id, new TimeOnly(9, 0), new TimeOnly(18, 0)) }), default);
        var newId = edit.Value;

        var result = await new ApproveScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveScheduleCommand(newId, Guid.NewGuid(), true), default);

        result.IsSuccess.Should().BeTrue();
        db.WorkSchedules.Single(s => s.Id == oldId).Status.Should().Be(WorkScheduleStatus.Superseded);
        db.WorkSchedules.Single(s => s.Id == newId).Status.Should().Be(WorkScheduleStatus.Approved);
        db.WorkSchedules.Count(s => s.UserId == pg.Id && s.Status == WorkScheduleStatus.Approved).Should().Be(1);
    }

    // ----- Withdraw -----

    [Fact]
    public async Task Withdraw_Pending_SoftDeletes()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var id = create.Value[0];

        var result = await new WithdrawScheduleCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new WithdrawScheduleCommand(id, pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.WorkSchedules.Any(s => s.Id == id).Should().BeFalse(); // filtered by soft-delete query filter
    }

    // ----- Approval scoping (BR-405/BR-406) -----

    [Fact]
    public async Task Approve_LeaderNotManaging_ReturnsNotApprover()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var otherLeader = Leader("other@example.com");
        db.Users.Add(otherLeader);
        await db.SaveChangesAsync();
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var id = create.Value[0];

        var result = await new ApproveScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveScheduleCommand(id, otherLeader.Id, false), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotApprover);
    }

    [Fact]
    public async Task Approve_ManagingLeader_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var leader = Leader();
        db.Users.Add(leader);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pg.Id, leader.Id, today));
        await db.SaveChangesAsync();
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var id = create.Value[0];

        var result = await new ApproveScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveScheduleCommand(id, leader.Id, false), default);

        result.IsSuccess.Should().BeTrue();
        db.WorkSchedules.Single(s => s.Id == id).Status.Should().Be(WorkScheduleStatus.Approved);
    }

    [Fact]
    public async Task Reject_SetsRejectedWithReason()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var create = await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(Future(clock), store.Id) }), default);
        var id = create.Value[0];

        var result = await new RejectScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new RejectScheduleCommand(id, Guid.NewGuid(), true, "Sai ca"), default);

        result.IsSuccess.Should().BeTrue();
        var schedule = db.WorkSchedules.Single(s => s.Id == id);
        schedule.Status.Should().Be(WorkScheduleStatus.Rejected);
        schedule.RejectReason.Should().Be("Sai ca");
    }

    // ----- Queries -----

    [Fact]
    public async Task GetMySchedule_ReturnsRowsInRange_WithStoreLabels()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var date = Future(clock);
        await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(date, store.Id) }), default);

        var result = await new GetMyScheduleQueryHandler(db)
            .Handle(new GetMyScheduleQuery(pg.Id, date.AddDays(-1), date.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Shifts.Single().StoreCode.Should().Be("ST-1");
        result.Value[0].Status.Should().Be("pending");
    }

    [Fact]
    public async Task GetUserSchedule_NonManagingLeader_ReturnsNotApprover()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, _) = await SeedPgWithStoreAsync(db, clock);

        var result = await new GetUserScheduleQueryHandler(db)
            .Handle(new GetUserScheduleQuery(Guid.NewGuid(), false, pg.Id, Future(clock), Future(clock, 10)), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotApprover);
    }

    [Fact]
    public async Task GetUserSchedule_Admin_BypassesScope()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var (pg, store) = await SeedPgWithStoreAsync(db, clock);
        var date = Future(clock);
        await new CreateScheduleCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new CreateScheduleCommand(pg.Id, new[] { Day(date, store.Id) }), default);

        var result = await new GetUserScheduleQueryHandler(db)
            .Handle(new GetUserScheduleQuery(Guid.NewGuid(), true, pg.Id, date.AddDays(-1), date.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}
