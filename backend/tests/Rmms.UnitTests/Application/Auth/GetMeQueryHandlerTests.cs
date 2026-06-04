using FluentAssertions;
using Rmms.Application.Auth.Me;
using Rmms.Domain.Devices;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class GetMeQueryHandlerTests
{
    [Fact]
    public async Task ReturnsProfile_WithoutDevice_WhenDeviceIdNull()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await new GetMeQueryHandler(db).Handle(new GetMeQuery(user.Id, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("pg@example.com");
        result.Value.CurrentDevice.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsProfile_WithCurrentDevice_WhenDeviceIdMatches()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        var device = UserDevice.RegisterFirstActive(
            user.Id, "dev-1", "iPhone 15", "ios", "17.0", "1.0.0", null,
            new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero));
        db.UserDevices.Add(device);
        await db.SaveChangesAsync();

        var result = await new GetMeQueryHandler(db).Handle(new GetMeQuery(user.Id, device.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentDevice.Should().NotBeNull();
        result.Value.CurrentDevice!.DeviceName.Should().Be("iPhone 15");
    }

    [Fact]
    public async Task UnknownUser_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await new GetMeQueryHandler(db).Handle(new GetMeQuery(Guid.NewGuid(), null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
