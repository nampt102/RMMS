using FluentAssertions;
using Rmms.Application.Admin.Users.AdminResetPassword;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Admin;

public sealed class AdminResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task ValidUser_IssuesTokenAndSendsResetEmail()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("alice@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var emailSender = new CapturingEmailSender();

        var sut = new AdminResetPasswordCommandHandler(
            db, emailSender, new FakeTemplateRenderer(), audit, clock);

        var result = await sut.Handle(new AdminResetPasswordCommand(user.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.PasswordResetTokens.Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);

        emailSender.Sent.Should().ContainSingle()
            .Which.ToEmail.Should().Be("alice@example.com");

        audit.Calls.Should().ContainSingle(c =>
            c.Action == AuditAction.UserPasswordResetRequested && c.TargetId == user.Id);
    }

    [Fact]
    public async Task UnknownUserId_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new AdminResetPasswordCommandHandler(
            db,
            new CapturingEmailSender(),
            new FakeTemplateRenderer(),
            new InMemoryAuditLogger(),
            new TestClock());

        var result = await sut.Handle(new AdminResetPasswordCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
        db.PasswordResetTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task AuditMetadata_FlagsTriggeredByAdmin()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = new AdminResetPasswordCommandHandler(
            db, new CapturingEmailSender(), new FakeTemplateRenderer(), audit, new TestClock());

        await sut.Handle(new AdminResetPasswordCommand(user.Id), default);

        var call = audit.Calls.Single();
        // metadata is an anonymous object — serialize it via JSON to inspect.
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(call.Metadata);
        metadataJson.Should().Contain("\"triggered_by\":\"admin\"");
    }

    [Fact]
    public async Task IssuedToken_HasExpiry24HoursFromNow()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var clock = new TestClock { UtcNow = new DateTimeOffset(2026, 06, 10, 12, 0, 0, TimeSpan.Zero) };
        var sut = new AdminResetPasswordCommandHandler(
            db, new CapturingEmailSender(), new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), clock);

        await sut.Handle(new AdminResetPasswordCommand(user.Id), default);

        db.PasswordResetTokens.Single().ExpiresAt
            .Should().Be(clock.UtcNow + TimeSpan.FromHours(24));
    }
}
