using FluentAssertions;
using Rmms.Application.Admin.Users.UpdateUser;
using Rmms.Domain.Auth;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Admin;

public sealed class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task UpdateProfile_AppliesNameAndPhone()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new UpdateUserCommandHandler(db, new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(
            new UpdateUserCommand(user.Id, FullName: "Tên Mới", Phone: "0908888777", Status: null, PreferredLanguage: "en"),
            default);

        result.IsSuccess.Should().BeTrue();
        var updated = db.Users.Single();
        updated.FullName.Should().Be("Tên Mới");
        updated.Phone.Should().Be("0908888777");
        updated.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task Deactivate_RevokesAllRefreshTokens()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);

        var deviceId = Guid.NewGuid();
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "rt-hash-1", DateTimeOffset.UtcNow, TimeSpan.FromDays(30)));
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "rt-hash-2", DateTimeOffset.UtcNow, TimeSpan.FromDays(30)));
        await db.SaveChangesAsync();

        var sut = new UpdateUserCommandHandler(db, new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(
            new UpdateUserCommand(user.Id, null, null, Status: "inactive", null),
            default);

        result.IsSuccess.Should().BeTrue();
        db.Users.Single().Status.Should().Be(UserStatus.Inactive);
        db.RefreshTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task Reactivate_FromInactiveToActive_Works()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreateInactivePg("u@example.com"));
        await db.SaveChangesAsync();

        var sut = new UpdateUserCommandHandler(db, new InMemoryAuditLogger(), new TestClock());
        var user = db.Users.Single();

        var result = await sut.Handle(
            new UpdateUserCommand(user.Id, null, null, Status: "active", null),
            default);

        result.IsSuccess.Should().BeTrue();
        db.Users.Single().Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task ActivatingPendingEmailVerify_ReturnsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreatePgPendingVerify();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new UpdateUserCommandHandler(db, new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(
            new UpdateUserCommand(user.Id, null, null, Status: "active", null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
        db.Users.Single().Status.Should().Be(UserStatus.PendingEmailVerify);
    }

    [Fact]
    public async Task UserNotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new UpdateUserCommandHandler(db, new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Anything", null, null, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task StatusChange_EmitsUserStatusChangedAuditWithFromTo()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = new UpdateUserCommandHandler(db, audit, new TestClock());

        await sut.Handle(new UpdateUserCommand(user.Id, null, null, "inactive", null), default);

        audit.Calls.Should().Contain(c => c.Action == AuditAction.UserStatusChanged);
        // Also the generic UserUpdatedByAdmin is always emitted
        audit.Calls.Should().Contain(c => c.Action == AuditAction.UserUpdatedByAdmin);
    }

    [Fact]
    public async Task NoStatusChange_EmitsOnlyUserUpdatedByAdmin()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = new UpdateUserCommandHandler(db, audit, new TestClock());

        await sut.Handle(new UpdateUserCommand(user.Id, "Tên Mới", null, null, null), default);

        audit.Calls.Should().NotContain(c => c.Action == AuditAction.UserStatusChanged);
        audit.Calls.Should().ContainSingle(c => c.Action == AuditAction.UserUpdatedByAdmin);
    }
}
