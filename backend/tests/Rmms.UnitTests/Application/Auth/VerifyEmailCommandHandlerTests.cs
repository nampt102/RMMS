using FluentAssertions;
using Rmms.Application.Auth.VerifyEmail;
using Rmms.Application.Common.Security;
using Rmms.Domain.Auth;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class VerifyEmailCommandHandlerTests
{
    private static VerifyEmailCommandHandler CreateSut(Infrastructure.Persistence.AppDbContext db, TestClock clock) =>
        new(db, new InMemoryAuditLogger(), clock);

    [Fact]
    public async Task ValidToken_VerifiesUser_AndMarksTokenUsed()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreatePgPendingVerify("pg@example.com");
        db.Users.Add(user);
        var (plaintext, hash) = OpaqueToken.Generate();
        db.EmailVerificationTokens.Add(EmailVerificationToken.Issue(user.Id, hash, clock.UtcNow, TimeSpan.FromHours(24)));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(new VerifyEmailCommand(plaintext), default);

        result.IsSuccess.Should().BeTrue();
        db.Users.Single().Status.Should().Be(UserStatus.Active);
        db.EmailVerificationTokens.Single().UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnknownToken_ReturnsTokenInvalid()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await CreateSut(db, new TestClock()).Handle(new VerifyEmailCommand("nope"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TokenInvalid);
    }

    [Fact]
    public async Task UsedToken_ReturnsEmailTokenUsed()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreatePgPendingVerify("pg@example.com");
        db.Users.Add(user);
        var (plaintext, hash) = OpaqueToken.Generate();
        var token = EmailVerificationToken.Issue(user.Id, hash, clock.UtcNow, TimeSpan.FromHours(24));
        token.MarkUsed(clock.UtcNow);
        db.EmailVerificationTokens.Add(token);
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(new VerifyEmailCommand(plaintext), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailTokenUsed);
    }

    [Fact]
    public async Task ExpiredToken_ReturnsEmailTokenExpired()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreatePgPendingVerify("pg@example.com");
        db.Users.Add(user);
        var (plaintext, hash) = OpaqueToken.Generate();
        db.EmailVerificationTokens.Add(
            EmailVerificationToken.Issue(user.Id, hash, clock.UtcNow.AddHours(-48), TimeSpan.FromHours(24)));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, clock).Handle(new VerifyEmailCommand(plaintext), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailTokenExpired);
    }
}
