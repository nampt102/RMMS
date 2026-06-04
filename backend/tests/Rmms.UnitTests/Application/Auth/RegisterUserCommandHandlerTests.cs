using FluentAssertions;
using Rmms.Application.Auth.Register;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Auth;

public sealed class RegisterUserCommandHandlerTests
{
    private static RegisterUserCommandHandler CreateSut(
        Infrastructure.Persistence.AppDbContext db, CapturingEmailSender email, TestClock clock) =>
        new(db, new FakePasswordHasher(), email, new FakeTemplateRenderer(), new InMemoryAuditLogger(), clock);

    [Fact]
    public async Task NewEmail_CreatesPendingUser_AndSendsVerificationEmail()
    {
        await using var db = TestDbContextFactory.Create();
        var email = new CapturingEmailSender();
        var clock = new TestClock();

        var result = await CreateSut(db, email, clock).Handle(
            new RegisterUserCommand("New@Example.com", "Passw0rd1", "New PG", "0900000000", "vi"), default);

        result.IsSuccess.Should().BeTrue();

        var user = db.Users.Single();
        user.Email.Should().Be("new@example.com", "email is normalized to lowercase");
        user.Status.Should().Be(UserStatus.PendingEmailVerify);
        user.Role.Should().Be(UserRole.Pg);

        db.EmailVerificationTokens.Should().ContainSingle();
        email.Sent.Should().ContainSingle().Which.Subject.Should().Contain("VERIFY");
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        var email = new CapturingEmailSender();
        var clock = new TestClock();
        db.Users.Add(UserFactory.CreateActivePg("dupe@example.com"));
        await db.SaveChangesAsync();

        var result = await CreateSut(db, email, clock).Handle(
            new RegisterUserCommand("dupe@example.com", "Passw0rd1", "Dup", null, "vi"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailAlreadyRegistered);
        email.Sent.Should().BeEmpty();
    }
}
