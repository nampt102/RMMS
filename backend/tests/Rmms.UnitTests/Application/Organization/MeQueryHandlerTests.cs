using FluentAssertions;
using Rmms.Application.Organization.Me;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class MeQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    [Fact]
    public async Task GetMyStores_ReturnsOnlyActiveAssignments()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var s1 = Store.Create("ST-1", "Active store", null, 10m, 106m, null);
        var s2 = Store.Create("ST-2", "Ended store", null, 11m, 107m, null);
        db.Users.Add(pg);
        db.Stores.AddRange(s1, s2);
        db.UserStoreAssignments.Add(UserStoreAssignment.Create(pg.Id, s1.Id, Today));
        var ended = UserStoreAssignment.Create(pg.Id, s2.Id, Today);
        ended.End(Today);
        db.UserStoreAssignments.Add(ended);
        await db.SaveChangesAsync();

        var result = await new GetMyStoresQueryHandler(db).Handle(new GetMyStoresQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.Code == "ST-1");
        result.Value.Single().Status.Should().Be("active");
    }

    [Fact]
    public async Task GetMyStores_NoAssignments_ReturnsEmpty()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var result = await new GetMyStoresQueryHandler(db).Handle(new GetMyStoresQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLeader_ReturnsActiveLeader()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        var leader = User.CreateByAdmin("leader@example.com", "plain:Pwd12345", "Leader X", UserRole.Leader, "0900000000", "vi");
        db.Users.AddRange(pg, leader);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pg.Id, leader.Id, Today));
        await db.SaveChangesAsync();

        var result = await new GetMyLeaderQueryHandler(db).Handle(new GetMyLeaderQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.LeaderUserId.Should().Be(leader.Id);
        result.Value.Email.Should().Be("leader@example.com");
    }

    [Fact]
    public async Task GetMyLeader_NoneAssigned_ReturnsNull()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var result = await new GetMyLeaderQueryHandler(db).Handle(new GetMyLeaderQuery(pg.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
