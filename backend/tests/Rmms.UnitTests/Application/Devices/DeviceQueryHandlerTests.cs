using FluentAssertions;
using Rmms.Application.Devices.GetMyDevice;
using Rmms.Application.Devices.GetPendingDevices;
using Rmms.Domain.Devices;
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

        var result = await new GetPendingDevicesQueryHandler(db).Handle(new GetPendingDevicesQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var dto = result.Value[0];
        dto.UserEmail.Should().Be("pg@example.com");
        dto.DeviceName.Should().Be("Pending");
        dto.UserRole.Should().Be("pg");
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
