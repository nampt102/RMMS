using FluentAssertions;
using Rmms.Application.Admin.Users.CreateAdminUser;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Admin;

public sealed class CreateAdminUserCommandHandlerTests
{
    private static CreateAdminUserCommandHandler BuildSut(
        out Rmms.Infrastructure.Persistence.AppDbContext db,
        out InMemoryAuditLogger audit,
        out CapturingEmailSender emailSender)
    {
        db = TestDbContextFactory.Create();
        audit = new InMemoryAuditLogger();
        emailSender = new CapturingEmailSender();
        return new CreateAdminUserCommandHandler(
            db,
            new FakePasswordHasher(),
            emailSender,
            new FakeTemplateRenderer(),
            audit);
    }

    [Theory]
    [InlineData("leader", UserRole.Leader)]
    [InlineData("buh", UserRole.Buh)]
    [InlineData("admin", UserRole.Admin)]
    public async Task ValidRole_CreatesActiveUser_EmailsInitialPassword(string role, UserRole expectedRole)
    {
        var sut = BuildSut(out var db, out var audit, out var emailSender);

        var result = await sut.Handle(
            new CreateAdminUserCommand("New@Example.com", "Người Mới", "0901111222", role, "vi"),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("new@example.com");
        result.Value.Role.Should().Be(role);
        result.Value.Status.Should().Be("active");

        // Active in DB
        db.Users.Should().ContainSingle(u => u.Role == expectedRole && u.Status == UserStatus.Active);

        // Initial password emailed (12-char body contains "pwd=")
        emailSender.Sent.Should().ContainSingle()
            .Which.BodyText.Should().Contain("pwd=");

        // Audit
        audit.Calls.Should().ContainSingle(c => c.Action == AuditAction.UserCreatedByAdmin);
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        var sut = BuildSut(out var db, out _, out _);
        db.Users.Add(UserFactory.CreateAdmin("dup@example.com"));
        await db.SaveChangesAsync();

        var result = await sut.Handle(
            new CreateAdminUserCommand("dup@example.com", "Same Email", null, "leader", "vi"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task DuplicateEmail_CaseInsensitive_DetectsExisting()
    {
        var sut = BuildSut(out var db, out _, out _);
        db.Users.Add(UserFactory.CreateAdmin("dup@example.com"));
        await db.SaveChangesAsync();

        var result = await sut.Handle(
            new CreateAdminUserCommand("  DUP@Example.com  ", "Same Email", null, "leader", "vi"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task InitialPassword_MeetsMinimumRules()
    {
        var sut = BuildSut(out _, out _, out var emailSender);

        await sut.Handle(
            new CreateAdminUserCommand("newuser@example.com", "Newbie", null, "leader", "vi"),
            default);

        var body = emailSender.Sent.Single().BodyText;
        var pwd = body.Split("pwd=")[1].Trim();
        pwd.Length.Should().BeGreaterThanOrEqualTo(8);
        pwd.Should().MatchRegex("[A-Za-z]"); // has a letter
        pwd.Should().MatchRegex("[0-9]");    // has a digit
    }

    [Fact]
    public async Task EmailIsNormalized_LowercaseAndTrimmed()
    {
        var sut = BuildSut(out var db, out _, out _);

        await sut.Handle(
            new CreateAdminUserCommand("  Mix.CASE@Example.COM  ", "Person", null, "leader", "vi"),
            default);

        db.Users.Single().Email.Should().Be("mix.case@example.com");
    }
}
