using FluentAssertions;
using Rmms.Application.Auth.Logout;
using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Auth;
using Rmms.Domain.Enums;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task ActiveToken_IsRevoked_AndAudited()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        db.RefreshTokens.Add(RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hash:rt", clock.UtcNow, TimeSpan.FromDays(30)));
        await db.SaveChangesAsync();

        var sut = new LogoutCommandHandler(db, new FakeRefreshTokenGenerator(), audit, clock);
        var result = await sut.Handle(new LogoutCommand("rt"), default);

        result.IsSuccess.Should().BeTrue();
        db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AuthLogout);
    }

    [Fact]
    public async Task AlreadyRevokedToken_IsIdempotent_NoSecondAudit()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var token = RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hash:rt", clock.UtcNow, TimeSpan.FromDays(30));
        token.Revoke(clock.UtcNow);
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        var sut = new LogoutCommandHandler(db, new FakeRefreshTokenGenerator(), audit, clock);
        var result = await sut.Handle(new LogoutCommand("rt"), default);

        result.IsSuccess.Should().BeTrue();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownToken_IsNoOpSuccess()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new LogoutCommandHandler(db, new FakeRefreshTokenGenerator(), new InMemoryAuditLogger(), new TestClock());

        var result = await sut.Handle(new LogoutCommand("does-not-exist"), default);

        result.IsSuccess.Should().BeTrue();
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public GeneratedRefreshToken Generate() => new("plain", "hash:plain");

        public string Hash(string plaintext) => $"hash:{plaintext}";
    }
}
