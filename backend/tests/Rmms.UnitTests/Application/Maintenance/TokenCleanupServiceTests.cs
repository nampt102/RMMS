using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rmms.Application.Maintenance;
using Rmms.Domain.Auth;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Maintenance;

public sealed class TokenCleanupServiceTests
{
    [Fact]
    public async Task RunAsync_DeletesUsedAndExpired_KeepsLiveAndRevokedUnexpired()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var now = clock.UtcNow;
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        // Email verification: live (keep), expired (delete), used (delete).
        db.EmailVerificationTokens.Add(EmailVerificationToken.Issue(userId, "e-live", now, TimeSpan.FromHours(24)));
        db.EmailVerificationTokens.Add(EmailVerificationToken.Issue(userId, "e-exp", now.AddHours(-48), TimeSpan.FromHours(24)));
        var usedEmail = EmailVerificationToken.Issue(userId, "e-used", now, TimeSpan.FromHours(24));
        usedEmail.MarkUsed(now);
        db.EmailVerificationTokens.Add(usedEmail);

        // Password reset: live (keep), expired (delete).
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(userId, "p-live", now, TimeSpan.FromHours(24)));
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(userId, "p-exp", now.AddHours(-48), TimeSpan.FromHours(24)));

        // Refresh: live (keep), expired (delete), revoked-but-unexpired (KEEP for reuse detection).
        db.RefreshTokens.Add(RefreshToken.Issue(userId, deviceId, "r-live", now, TimeSpan.FromDays(30)));
        db.RefreshTokens.Add(RefreshToken.Issue(userId, deviceId, "r-exp", now.AddDays(-31), TimeSpan.FromDays(30)));
        var revoked = RefreshToken.Issue(userId, deviceId, "r-revoked", now, TimeSpan.FromDays(30));
        revoked.Revoke(now);
        db.RefreshTokens.Add(revoked);

        await db.SaveChangesAsync();

        var sut = new TokenCleanupService(db, clock, NullLogger<TokenCleanupService>.Instance);

        var result = await sut.RunAsync();

        result.EmailVerificationTokens.Should().Be(2);
        result.PasswordResetTokens.Should().Be(1);
        result.RefreshTokens.Should().Be(1);
        result.Total.Should().Be(4);

        db.EmailVerificationTokens.Should().ContainSingle().Which.TokenHash.Should().Be("e-live");
        db.PasswordResetTokens.Should().ContainSingle().Which.TokenHash.Should().Be("p-live");
        db.RefreshTokens.Select(t => t.TokenHash).Should().BeEquivalentTo("r-live", "r-revoked");
    }

    [Fact]
    public async Task RunAsync_NothingToDelete_ReturnsZeros()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        db.EmailVerificationTokens.Add(
            EmailVerificationToken.Issue(Guid.NewGuid(), "live", clock.UtcNow, TimeSpan.FromHours(24)));
        await db.SaveChangesAsync();

        var sut = new TokenCleanupService(db, clock, NullLogger<TokenCleanupService>.Instance);

        var result = await sut.RunAsync();

        result.Total.Should().Be(0);
        db.EmailVerificationTokens.Should().HaveCount(1);
    }
}
