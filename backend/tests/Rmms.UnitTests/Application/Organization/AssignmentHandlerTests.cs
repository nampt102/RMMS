using FluentAssertions;
using Rmms.Application.Organization.Assignments;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class AssignmentHandlerTests
{
    private static User Leader(string email = "leader@example.com") =>
        User.CreateByAdmin(email, "plain:Pwd12345", "Leader X", UserRole.Leader, null, "vi");

    // ----- PG -> Leader -----

    [Fact]
    public async Task AssignPgLeader_Succeeds_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var leader = Leader();
        db.Users.AddRange(pg, leader);
        await db.SaveChangesAsync();
        var audit = new InMemoryAuditLogger();

        var result = await new AssignPgLeaderCommandHandler(db, audit, clock)
            .Handle(new AssignPgLeaderCommand(pg.Id, leader.Id), default);

        result.IsSuccess.Should().BeTrue();
        var a = db.UserLeaderAssignments.Single();
        a.PgUserId.Should().Be(pg.Id);
        a.LeaderUserId.Should().Be(leader.Id);
        a.EffectiveTo.Should().BeNull();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.PgLeaderAssigned);
    }

    [Fact]
    public async Task AssignPgLeader_TargetNotPg_ReturnsInvalidAssignment()
    {
        await using var db = TestDbContextFactory.Create();
        var notPg = Leader("notpg@example.com");
        var leader = Leader();
        db.Users.AddRange(notPg, leader);
        await db.SaveChangesAsync();

        var result = await new AssignPgLeaderCommandHandler(db, new InMemoryAuditLogger(), new TestClock())
            .Handle(new AssignPgLeaderCommand(notPg.Id, leader.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidAssignment);
    }

    [Fact]
    public async Task AssignPgLeader_LeaderNotLeaderRole_ReturnsInvalidAssignment()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var notLeader = UserFactory.CreateActivePg("alsopg@example.com");
        db.Users.AddRange(pg, notLeader);
        await db.SaveChangesAsync();

        var result = await new AssignPgLeaderCommandHandler(db, new InMemoryAuditLogger(), new TestClock())
            .Handle(new AssignPgLeaderCommand(pg.Id, notLeader.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidAssignment);
    }

    [Fact]
    public async Task AssignPgLeader_Reassign_EndsPreviousActive()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var leaderA = Leader("a@example.com");
        var leaderB = Leader("b@example.com");
        db.Users.AddRange(pg, leaderA, leaderB);
        db.UserLeaderAssignments.Add(
            UserLeaderAssignment.Create(pg.Id, leaderA.Id, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        await db.SaveChangesAsync();

        var result = await new AssignPgLeaderCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new AssignPgLeaderCommand(pg.Id, leaderB.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.UserLeaderAssignments.Should().HaveCount(2);
        db.UserLeaderAssignments.Single(a => a.LeaderUserId == leaderA.Id).EffectiveTo.Should().NotBeNull();
        db.UserLeaderAssignments.Single(a => a.LeaderUserId == leaderB.Id).EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task AssignPgLeader_SameLeader_IsIdempotentNoOp()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var leader = Leader();
        db.Users.AddRange(pg, leader);
        db.UserLeaderAssignments.Add(
            UserLeaderAssignment.Create(pg.Id, leader.Id, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        await db.SaveChangesAsync();

        var result = await new AssignPgLeaderCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new AssignPgLeaderCommand(pg.Id, leader.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.UserLeaderAssignments.Should().ContainSingle();
    }

    // ----- User -> Store -----

    [Fact]
    public async Task AssignUserStore_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var store = Store.Create("ST-1", "S", null, 1m, 1m, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var result = await new AssignUserStoreCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new AssignUserStoreCommand(pg.Id, store.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.UserStoreAssignments.Should().ContainSingle();
    }

    [Fact]
    public async Task AssignUserStore_Duplicate_ReturnsAssignmentExists()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var store = Store.Create("ST-1", "S", null, 1m, 1m, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        db.UserStoreAssignments.Add(
            UserStoreAssignment.Create(pg.Id, store.Id, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        await db.SaveChangesAsync();

        var result = await new AssignUserStoreCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new AssignUserStoreCommand(pg.Id, store.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AssignmentExists);
    }

    [Fact]
    public async Task AssignUserStore_UnknownStore_ReturnsInvalidReference()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var result = await new AssignUserStoreCommandHandler(db, new InMemoryAuditLogger(), new TestClock())
            .Handle(new AssignUserStoreCommand(pg.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidReference);
    }

    [Fact]
    public async Task UnassignUserStore_EndsActiveAssignment()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var store = Store.Create("ST-1", "S", null, 1m, 1m, null);
        db.Users.Add(pg);
        db.Stores.Add(store);
        db.UserStoreAssignments.Add(
            UserStoreAssignment.Create(pg.Id, store.Id, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        await db.SaveChangesAsync();

        var result = await new UnassignUserStoreCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new UnassignUserStoreCommand(pg.Id, store.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.UserStoreAssignments.Single().EffectiveTo.Should().NotBeNull();
    }

    [Fact]
    public async Task UnassignUserStore_NotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new UnassignUserStoreCommandHandler(db, new InMemoryAuditLogger(), new TestClock())
            .Handle(new UnassignUserStoreCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }

    // ----- User -> Category -----

    [Fact]
    public async Task AssignUserCategory_Succeeds_ThenDuplicateConflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var cat = Category.Create("BEV", "Beverages");
        db.Users.Add(pg);
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var first = await new AssignUserCategoryCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new AssignUserCategoryCommand(pg.Id, cat.Id), default);
        first.IsSuccess.Should().BeTrue();

        var dup = await new AssignUserCategoryCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new AssignUserCategoryCommand(pg.Id, cat.Id), default);
        dup.IsFailure.Should().BeTrue();
        dup.Error.Code.Should().Be(ErrorCodes.AssignmentExists);
    }

    [Fact]
    public async Task UnassignUserCategory_RemovesLink()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var cat = Category.Create("BEV", "Beverages");
        db.Users.Add(pg);
        db.Categories.Add(cat);
        db.UserCategoryAssignments.Add(UserCategoryAssignment.Create(pg.Id, cat.Id));
        await db.SaveChangesAsync();

        var result = await new UnassignUserCategoryCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UnassignUserCategoryCommand(pg.Id, cat.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.UserCategoryAssignments.Any().Should().BeFalse();
    }

    // ----- Query -----

    [Fact]
    public async Task GetUserAssignments_ReturnsLeaderStoresCategories()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var leader = Leader();
        var store = Store.Create("ST-1", "Store 1", null, 1m, 1m, null);
        var cat = Category.Create("BEV", "Beverages");
        db.Users.AddRange(pg, leader);
        db.Stores.Add(store);
        db.Categories.Add(cat);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pg.Id, leader.Id, today));
        db.UserStoreAssignments.Add(UserStoreAssignment.Create(pg.Id, store.Id, today));
        db.UserCategoryAssignments.Add(UserCategoryAssignment.Create(pg.Id, cat.Id));
        await db.SaveChangesAsync();

        var result = await new GetUserAssignmentsQueryHandler(db).Handle(new GetUserAssignmentsQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Leader!.LeaderUserId.Should().Be(leader.Id);
        result.Value.Stores.Should().ContainSingle(s => s.Code == "ST-1");
        result.Value.Categories.Should().ContainSingle(c => c.Code == "BEV");
    }

    [Fact]
    public async Task GetUserAssignments_UnknownUser_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new GetUserAssignmentsQueryHandler(db).Handle(new GetUserAssignmentsQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
