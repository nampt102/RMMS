using FluentAssertions;
using Rmms.Application.Organization.Views;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class OrgViewsHandlerTests
{
    private static User Leader(string email = "leader@example.com") =>
        User.CreateByAdmin(email, "plain:Pwd12345", "Leader X", UserRole.Leader);

    private static User Buh(string email = "buh@example.com") =>
        User.CreateByAdmin(email, "plain:Pwd12345", "BUH X", UserRole.Buh);

    [Fact]
    public async Task Hierarchy_GroupsPgsUnderLeader_AndSurfacesUnassigned()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var buh = Buh();
        var leader = Leader();
        var pgAssigned = UserFactory.CreateActivePg("pg1@example.com");
        var pgUnassigned = UserFactory.CreateActivePg("pg2@example.com");
        db.Users.AddRange(buh, leader, pgAssigned, pgUnassigned);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pgAssigned.Id, leader.Id, today));
        await db.SaveChangesAsync();

        var result = await new GetOrgHierarchyQueryHandler(db).Handle(new GetOrgHierarchyQuery(), default);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Buhs.Should().ContainSingle().Which.Role.Should().Be("buh");
        dto.Leaders.Should().ContainSingle();
        dto.Leaders[0].Pgs.Should().ContainSingle().Which.Id.Should().Be(pgAssigned.Id);
        dto.UnassignedPgs.Should().ContainSingle().Which.Id.Should().Be(pgUnassigned.Id);
    }

    [Fact]
    public async Task AreaTree_NestsStoresAndEmployees_AndSurfacesAreaslessStores()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var area = Area.Create("HCM", "Hồ Chí Minh", null);
        var inArea = Store.Create("ST-1", "Store 1", null, 10m, 106m, area.Id);
        var noArea = Store.Create("ST-2", "Store 2", null, 11m, 107m, null);
        var pg = UserFactory.CreateActivePg("pg1@example.com");
        db.Areas.Add(area);
        db.Stores.AddRange(inArea, noArea);
        db.Users.Add(pg);
        db.UserStoreAssignments.Add(UserStoreAssignment.Create(pg.Id, inArea.Id, today));
        await db.SaveChangesAsync();

        var result = await new GetOrgAreaTreeQueryHandler(db).Handle(new GetOrgAreaTreeQuery(), default);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Areas.Should().ContainSingle();
        dto.Areas[0].Stores.Should().ContainSingle().Which.Employees.Should().ContainSingle()
            .Which.Id.Should().Be(pg.Id);
        dto.UnassignedStores.Should().ContainSingle().Which.Code.Should().Be("ST-2");
    }
}
