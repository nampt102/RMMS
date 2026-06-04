using FluentAssertions;
using Microsoft.Extensions.Options;
using Rmms.Application.Auth.Login;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Options;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class LoginCommandHandlerTests
{
    private static LoginCommandHandler CreateSut(AppDbContext db, TestClock clock) =>
        new(
            db,
            new FakePasswordHasher(),
            new FakeJwtTokenService(),
            new FakeRefreshTokenGenerator(),
            new InMemoryAuditLogger(),
            new TestClientContext(),
            clock,
            Options.Create(new JwtOptions { RefreshTokenDays = 30 }));

    private static LoginDeviceInfo Device(string deviceId = "dev-1") =>
        new(deviceId, "iPhone 15", "ios", "17.0", "1.0.0", null);

    // ----- Device check (BR-105) -----

    [Fact]
    public async Task Pg_FirstDevice_AutoRegistersActive_AndSucceeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", Device()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("pg@example.com");
        db.UserDevices.Should().ContainSingle()
            .Which.Status.Should().Be(DeviceStatus.Active);
        db.RefreshTokens.Should().ContainSingle();
        db.LoginHistory.Should().ContainSingle().Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Pg_SameActiveDevice_ReusesAndSucceeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        db.UserDevices.Add(UserDevice.RegisterFirstActive(
            user.Id, "dev-1", "iPhone 15", "ios", "17.0", "1.0.0", null, clock.UtcNow));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", Device("dev-1")), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Should().ContainSingle("the existing device is reused, not duplicated");
    }

    [Fact]
    public async Task Pg_DifferentDevice_ReturnsDeviceNotAuthorized_AndCreatesPending()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        db.UserDevices.Add(UserDevice.RegisterFirstActive(
            user.Id, "dev-1", "iPhone 15", "ios", "17.0", "1.0.0", null, clock.UtcNow));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", Device("dev-2")), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DeviceNotAuthorized);
        db.UserDevices.Should().Contain(d => d.DeviceId == "dev-2" && d.Status == DeviceStatus.PendingApproval);
        db.RefreshTokens.Should().BeEmpty("no tokens are issued when the device is blocked");
    }

    [Fact]
    public async Task NonPg_WithoutDevice_Succeeds_WithNoDeviceBoundToken()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var admin = UserFactory.CreateAdmin("admin@example.com");
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("admin@example.com", "AdminPwd1", null), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Should().BeEmpty("web users are not device-locked (BR-105 is PG-only)");
        db.RefreshTokens.Should().ContainSingle().Which.DeviceId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Pg_WithoutDevice_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DeviceNotAuthorized);
    }

    // ----- Status / credential gates -----

    [Fact]
    public async Task WrongPassword_ReturnsInvalidCredentials()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        db.Users.Add(UserFactory.CreateActivePg("pg@example.com"));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "WrongPwd", Device()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsInvalidCredentials()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("ghost@example.com", "whatever1", Device()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task PendingVerify_ReturnsEmailNotVerified()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        db.Users.Add(UserFactory.CreatePgPendingVerify("pg@example.com"));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", Device()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailNotVerified);
    }

    [Fact]
    public async Task InactiveAccount_ReturnsAccountInactive()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        db.Users.Add(UserFactory.CreateInactivePg("pg@example.com"));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(
            new LoginCommand("pg@example.com", "Test1234", Device()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AccountInactive);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public IssuedAccessToken IssueAccess(Guid userId, string email, UserRole role, Guid deviceId, DateTimeOffset now)
            => new("access-jwt", now.AddMinutes(15));
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public GeneratedRefreshToken Generate() => new("plain-refresh", "hash-refresh");

        public string Hash(string plaintext) => $"hash:{plaintext}";
    }
}
