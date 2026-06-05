using FluentAssertions;
using Rmms.Application.Devices.GetMyDevice;
using Rmms.Application.Devices.GetPendingDevices;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Devices;

public sealed class DeviceQueryHandlerTests
{
    [Fact]
    public async Task GetPending_ReturnsOnlyPending_WithOwnerInfo()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(pg);
        db.UserDevices.Add(UserDevice.RegisterFirstActive(pg.Id, "dev-active", "Active", "ios", "17", "1.0.0", null, clock.UtcNow));
        db.UserDevices.Add(UserDevice.RegisterPendingApproval(pg.Id, "dev-pending", "Pending", "android", "14", "1.0.0", null));
        await db.SaveChangesAsync();

        var result = await new GetPendingDevicesQueryHandler(db)
            .Handle(new GetPendingDevicesQuery(Guid.NewGuid(), IsAdmin: true), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var dto = result.Value[0];
        dto.UserEmail.Should().Be("pg@example.com");
        dto.DeviceName.Should().Be("Pending");
        dto.UserRole.Should().Be("pg");
    }

    [Fact]
    public async Task GetPending_AsLeader_ReturnsOnlyManagedPgRequests()
    {
        await using var db = TestDbContextFactory.Create();
        var today = new DateOnly(2026, 6, 1);
        var leader = User.CreateByAdmin("leader@example.com", "plain:Pwd12345", "Leader", UserRole.Leader, null, "vi");
        var managedPg = UserFactory.CreateActivePg("managed@example.com");
        var otherPg = UserFactory.CreateActivePg("other@example.com");
        db.Users.AddRange(leader, managedPg, otherPg);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(managedPg.Id, leader.Id, today));
        db.UserDevices.Add(UserDevice.RegisterPendingApproval(managedPg.Id, "dev-managed", "Managed", "android", "14", "1.0.0", null));
        db.UserDevices.Add(UserDevice.RegisterPendingApproval(otherPg.Id, "dev-other", "Other", "android", "14", "1.0.0", null));
        await db.SaveChangesAsync();

        var result = await new GetPendingDevicesQueryHandler(db)
            .Handle(new GetPendingDevicesQuery(leader.Id, IsAdmin: false), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].UserEmail.Should().Be("managed@example.com");
    }

    [Fact]
    public async Task GetMyDevice_ReturnsActiveAndPending()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var userId = Guid.NewGuid();
        db.UserDevices.Add(UserDevice.RegisterFirstActive(userId, "dev-a", "Active", "ios", "17", "1.0.0", null, clock.UtcNow));
        db.UserDevices.Add(UserDevice.RegisterPendingApproval(userId, "dev-p", "Pending", "android", "14", "1.0.0", null));
        await db.SaveChangesAsync();

        var result = await new GetMyDeviceQueryHandler(db).Handle(new GetMyDeviceQuery(userId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().NotBeNull();
        result.Value.Active!.DeviceId.Should().Be("dev-a");
        result.Value.Pending.Should().NotBeNull();
        result.Value.Pending!.DeviceId.Should().Be("dev-p");
    }

    [Fact]
    public async Task GetMyDevice_NoDevices_ReturnsNulls()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await new GetMyDeviceQueryHandler(db).Handle(new GetMyDeviceQuery(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeNull();
        result.Value.Pending.Should().BeNull();
    }
}
