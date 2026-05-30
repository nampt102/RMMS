using FluentAssertions;
using Microsoft.Extensions.Options;
using Rmms.Application.Auth.Refresh;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Options;
using Rmms.Domain.Audit;
using Rmms.Domain.Auth;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private static RefreshTokenCommandHandler CreateSut(TestClock clock, InMemoryAuditLogger audit, AppDbContext db) =>
        new(
            db,
            new FakeJwtTokenService(),
            new FakeRefreshTokenGenerator(),
            audit,
            clock,
            Options.Create(new JwtOptions { RefreshTokenDays = 30 }));

    [Fact]
    public async Task ReusedRevokedToken_RevokesAllActiveTokens_AndReturnsReused()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var now = clock.UtcNow;
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);

        var deviceId = Guid.NewGuid();

        // The already-rotated (revoked) token being replayed.
        var reused = RefreshToken.Issue(user.Id, deviceId, "hash:reused", now, TimeSpan.FromDays(30));
        reused.Revoke(now);
        db.RefreshTokens.Add(reused);

        // Two still-active sessions that MUST be nuked when reuse is detected.
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "hash:active1", now, TimeSpan.FromDays(30)));
        db.RefreshTokens.Add(RefreshToken.Issue(user.Id, deviceId, "hash:active2", now, TimeSpan.FromDays(30)));
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = CreateSut(clock, audit, db);

        var result = await sut.Handle(new RefreshTokenCommand("reused"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RefreshTokenReused);

        db.RefreshTokens.Should().OnlyContain(t => t.RevokedAt != null,
            "reuse detection must revoke every refresh token for the user");
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AuthRefreshReused);
    }

    [Fact]
    public async Task ValidToken_RotatesAndRevokesOld()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var now = clock.UtcNow;
        var user = UserFactory.CreateActivePg("pg@example.com");
        db.Users.Add(user);

        var deviceId = Guid.NewGuid();
        var current = RefreshToken.Issue(user.Id, deviceId, "hash:valid", now, TimeSpan.FromDays(30));
        db.RefreshTokens.Add(current);
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var sut = CreateSut(clock, audit, db);

        var result = await sut.Handle(new RefreshTokenCommand("valid"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("plain-new");

        var old = db.RefreshTokens.Single(t => t.TokenHash == "hash:valid");
        old.RevokedAt.Should().NotBeNull("the old token is revoked on rotation");
        old.ReplacedByTokenId.Should().NotBeNull();

        db.RefreshTokens.Should().Contain(t => t.TokenHash == "hash:new" && t.RevokedAt == null);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AuthRefreshRotated);
    }

    [Fact]
    public async Task UnknownToken_ReturnsRevoked()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var sut = CreateSut(clock, audit, db);

        var result = await sut.Handle(new RefreshTokenCommand("does-not-exist"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RefreshTokenRevoked);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public IssuedAccessToken IssueAccess(Guid userId, string email, UserRole role, Guid deviceId, DateTimeOffset now)
            => new("access-jwt", now.AddMinutes(15));
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public GeneratedRefreshToken Generate() => new("plain-new", "hash:new");

        public string Hash(string plaintext) => $"hash:{plaintext}";
    }
}
