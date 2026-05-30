using FluentAssertions;
using Rmms.Application.Auth.Me;
using Rmms.Domain.Devices;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class GetDeviceStatusQueryHandlerTests
{
    [Theory]
    [InlineData(false)] // null device id
    [InlineData(true)]  // Guid.Empty device id (web token)
    public async Task WebUser_ReturnsNone(bool empty)
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new GetDeviceStatusQueryHandler(db);

        var result = await sut.Handle(
            new GetDeviceStatusQuery(Guid.NewGuid(), empty ? Guid.Empty : null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("none");
        result.Value.DeviceId.Should().BeNull();
    }

    [Fact]
    public async Task ActiveDevice_ReturnsActiveStatusAndName()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var device = UserDevice.RegisterFirstActive(
            userId, "dev-1", "iPhone 15", "ios", "17.0", "1.0.0", null,
            new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero));
        db.UserDevices.Add(device);
        await db.SaveChangesAsync();

        var sut = new GetDeviceStatusQueryHandler(db);

        var result = await sut.Handle(new GetDeviceStatusQuery(userId, device.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("active");
        result.Value.DeviceId.Should().Be(device.Id);
        result.Value.DeviceName.Should().Be("iPhone 15");
    }

    [Fact]
    public async Task UnknownDevice_ReturnsUnknown()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new GetDeviceStatusQueryHandler(db);

        var result = await sut.Handle(new GetDeviceStatusQuery(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("unknown");
    }
}
