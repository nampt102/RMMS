using FluentAssertions;
using Rmms.Application.Auth.ForgotPassword;
using Rmms.Application.Common.Security;
using Rmms.Domain.Enums;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task ActiveUser_IssuesTokenAndSendsEmail()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("active@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var clock = new TestClock();
        var emailSender = new CapturingEmailSender();
        var audit = new InMemoryAuditLogger();

        var sut = new ForgotPasswordCommandHandler(db, emailSender, new FakeTemplateRenderer(), audit, clock);

        // Act
        var result = await sut.Handle(new ForgotPasswordCommand("active@example.com"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // A reset token row was created
        db.PasswordResetTokens.Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);

        // Email was queued, body contains plaintext token (not hash)
        emailSender.Sent.Should().ContainSingle()
            .Which.BodyText.Should().StartWith("reset token=");

        // Audit row emitted
        audit.Calls.Should().ContainSingle(c => c.Action == AuditAction.UserPasswordResetRequested);
    }

    [Fact]
    public async Task UnknownEmail_StillReturnsSuccess_NoTokenEmitted()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new ForgotPasswordCommandHandler(
            db, new CapturingEmailSender(), new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new ForgotPasswordCommand("ghost@example.com"), default);

        result.IsSuccess.Should().BeTrue();
        db.PasswordResetTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task InactiveUser_StillReturnsSuccess_NoTokenEmitted()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreateInactivePg("inactive@example.com"));
        await db.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var sut = new ForgotPasswordCommandHandler(
            db, emailSender, new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new ForgotPasswordCommand("inactive@example.com"), default);

        result.IsSuccess.Should().BeTrue();
        db.PasswordResetTokens.Should().BeEmpty();
        emailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task PendingEmailVerifyUser_StillReturnsSuccess_NoTokenEmitted()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreatePgPendingVerify("pending@example.com"));
        await db.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var sut = new ForgotPasswordCommandHandler(
            db, emailSender, new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new ForgotPasswordCommand("pending@example.com"), default);

        result.IsSuccess.Should().BeTrue();
        db.PasswordResetTokens.Should().BeEmpty();
        emailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task IssuedToken_HasExpiry24HoursFromNow()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreateActivePg("active@example.com"));
        await db.SaveChangesAsync();

        var clock = new TestClock { UtcNow = new DateTimeOffset(2026, 06, 01, 10, 0, 0, TimeSpan.Zero) };
        var sut = new ForgotPasswordCommandHandler(
            db, new CapturingEmailSender(), new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), clock);

        await sut.Handle(new ForgotPasswordCommand("active@example.com"), default);

        var token = db.PasswordResetTokens.Single();
        token.ExpiresAt.Should().Be(clock.UtcNow + TimeSpan.FromHours(24));
        token.CreatedAt.Should().Be(clock.UtcNow);
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public async Task EmailNormalization_LowercaseAndTrim_Matches()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreateActivePg("active@example.com"));
        await db.SaveChangesAsync();

        var sut = new ForgotPasswordCommandHandler(
            db, new CapturingEmailSender(), new FakeTemplateRenderer(),
            new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new ForgotPasswordCommand("  Active@Example.com  "), default);

        result.IsSuccess.Should().BeTrue();
        db.PasswordResetTokens.Should().ContainSingle();
    }
}
