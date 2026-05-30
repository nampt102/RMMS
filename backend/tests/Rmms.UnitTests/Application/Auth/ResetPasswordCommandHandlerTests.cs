using FluentAssertions;
using Rmms.Application.Auth.ResetPassword;
using Rmms.Application.Common.Security;
using Rmms.Domain.Auth;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task ValidToken_AppliesNewPasswordAndRevokesAllRefreshTokens()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("user@example.com");
        db.Users.Add(user);

        var clock = new TestClock();
        var (plaintext, hash) = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(
            PasswordResetToken.Issue(user.Id, hash, clock.UtcNow, TimeSpan.FromHours(24)));

        // Two active refresh tokens that should be revoked
        var deviceId = Guid.NewGuid();
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "rt-hash-1", clock.UtcNow, TimeSpan.FromDays(30)));
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "rt-hash-2", clock.UtcNow, TimeSpan.FromDays(30)));

        await db.SaveChangesAsync();

        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), new InMemoryAuditLogger(), clock);

        // Act
        var result = await sut.Handle(new ResetPasswordCommand(plaintext, "NewPwd123"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // New password applied (FakePasswordHasher format)
        db.Users.Single().PasswordHash.Should().Be("plain:NewPwd123");

        // Token marked used
        db.PasswordResetTokens.Single().UsedAt.Should().NotBeNull();

        // All refresh tokens revoked
        db.RefreshTokens.Should().HaveCount(2);
        db.RefreshTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task UnknownToken_ReturnsTokenInvalid()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new ResetPasswordCommand("never-existed-token-abc", "NewPwd123"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TokenInvalid);
    }

    [Fact]
    public async Task UsedToken_ReturnsEmailTokenUsed()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);

        var clock = new TestClock();
        var (plaintext, hash) = OpaqueToken.Generate();
        var token = PasswordResetToken.Issue(user.Id, hash, clock.UtcNow, TimeSpan.FromHours(24));
        token.MarkUsed(clock.UtcNow);
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync();

        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), new InMemoryAuditLogger(), clock);

        var result = await sut.Handle(new ResetPasswordCommand(plaintext, "NewPwd123"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailTokenUsed);
    }

    [Fact]
    public async Task ExpiredToken_ReturnsEmailTokenExpired()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);

        var clock = new TestClock();
        var issuedAt = clock.UtcNow - TimeSpan.FromHours(25);
        var (plaintext, hash) = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, hash, issuedAt, TimeSpan.FromHours(24)));
        await db.SaveChangesAsync();

        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), new InMemoryAuditLogger(), clock);

        var result = await sut.Handle(new ResetPasswordCommand(plaintext, "NewPwd123"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailTokenExpired);
    }

    [Fact]
    public async Task UserNotFound_ReturnsTokenInvalid()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        // Token references a user that doesn't exist
        var (plaintext, hash) = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(Guid.NewGuid(), hash, clock.UtcNow, TimeSpan.FromHours(24)));
        await db.SaveChangesAsync();

        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), new InMemoryAuditLogger(), clock);

        var result = await sut.Handle(new ResetPasswordCommand(plaintext, "NewPwd123"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TokenInvalid);
    }

    [Fact]
    public async Task AuditLog_RecordsUserPasswordReset_WithRevokeCount()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);

        var clock = new TestClock();
        var (plaintext, hash) = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, hash, clock.UtcNow, TimeSpan.FromHours(24)));
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, Guid.NewGuid(), "rt-hash-x", clock.UtcNow, TimeSpan.FromDays(30)));
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = new ResetPasswordCommandHandler(db, new FakePasswordHasher(), audit, clock);

        await sut.Handle(new ResetPasswordCommand(plaintext, "NewPwd123"), default);

        audit.Calls.Should().ContainSingle(c => c.Action == AuditAction.UserPasswordReset && c.TargetId == user.Id);
    }
}
