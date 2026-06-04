using FluentAssertions;
using Rmms.Application.Admin.Users.CreateAdminUser;
using Rmms.Application.Admin.Users.UpdateUser;
using Rmms.Application.Auth.ForgotPassword;
using Rmms.Application.Auth.Login;
using Rmms.Application.Auth.Logout;
using Rmms.Application.Auth.Refresh;
using Rmms.Application.Auth.Register;
using Rmms.Application.Auth.ResetPassword;
using Rmms.Application.Auth.VerifyEmail;
using Xunit;

namespace Rmms.UnitTests.Application.Validation;

/// <summary>
/// Direct FluentValidation tests for the M01 command validators (the input-validation
/// critical paths that the handler tests bypass).
/// </summary>
public sealed class CommandValidatorTests
{
    private static LoginDeviceInfo Device() => new("dev-1", "iPhone", "ios", "17.0", "1.0.0", null);

    // ----- Register -----

    [Fact]
    public void Register_Valid_Passes()
    {
        var r = new RegisterUserCommandValidator().Validate(
            new RegisterUserCommand("a@b.co", "Passw0rd1", "New PG", null, "vi"));
        r.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("a@b.co", "short1", "Name", "vi")]      // < 8 chars
    [InlineData("a@b.co", "password", "Name", "vi")]    // no digit
    [InlineData("a@b.co", "12345678", "Name", "vi")]    // no letter
    public void Register_WeakPassword_Fails(string email, string pwd, string name, string lang)
    {
        var r = new RegisterUserCommandValidator().Validate(new RegisterUserCommand(email, pwd, name, null, lang));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorCode == "PASSWORD_TOO_WEAK");
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd1", "Name", "vi")]
    [InlineData("a@b.co", "Passw0rd1", "", "vi")]       // empty name
    [InlineData("a@b.co", "Passw0rd1", "Name", "fr")]   // unsupported language
    public void Register_InvalidFields_Fail(string email, string pwd, string name, string lang)
    {
        var r = new RegisterUserCommandValidator().Validate(new RegisterUserCommand(email, pwd, name, null, lang));
        r.IsValid.Should().BeFalse();
    }

    // ----- Login -----

    [Fact]
    public void Login_Valid_Passes()
    {
        new LoginCommandValidator().Validate(new LoginCommand("a@b.co", "anything", Device()))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Login_NoDevice_IsAllowed_DeviceRequirementEnforcedInHandler()
    {
        new LoginCommandValidator().Validate(new LoginCommand("a@b.co", "anything", null))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Login_BadOs_Fails()
    {
        var bad = new LoginCommand("a@b.co", "anything", new LoginDeviceInfo("d", "n", "web", "1", "1", null));
        new LoginCommandValidator().Validate(bad).IsValid.Should().BeFalse();
    }

    // ----- Reset / Forgot / Logout / Refresh / VerifyEmail -----

    [Fact]
    public void ResetPassword_WeakPassword_Fails()
    {
        var r = new ResetPasswordCommandValidator().Validate(new ResetPasswordCommand("abc1234567", "weak"));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorCode == "PASSWORD_TOO_WEAK");
    }

    [Fact]
    public void ResetPassword_Valid_Passes()
    {
        new ResetPasswordCommandValidator().Validate(new ResetPasswordCommand("abcdef1234", "NewPass12"))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void ForgotPassword_BadEmail_Fails()
    {
        new ForgotPasswordCommandValidator().Validate(new ForgotPasswordCommand("nope")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Logout_Empty_Fails()
    {
        new LogoutCommandValidator().Validate(new LogoutCommand("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Refresh_TooShort_Fails()
    {
        new RefreshTokenCommandValidator().Validate(new RefreshTokenCommand("short")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyEmail_Valid_Passes()
    {
        new VerifyEmailCommandValidator().Validate(new VerifyEmailCommand("0123456789abcdef")).IsValid.Should().BeTrue();
    }

    // ----- Admin -----

    [Fact]
    public void CreateAdminUser_Valid_Passes()
    {
        var cmd = new CreateAdminUserCommand(Email: "leader@x.co", FullName: "Leader", Phone: null, Role: "leader", PreferredLanguage: "vi");
        new CreateAdminUserCommandValidator().Validate(cmd).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("pg")]      // PG must self-register
    [InlineData("owner")]   // unknown role
    public void CreateAdminUser_InvalidRole_Fails(string role)
    {
        var cmd = new CreateAdminUserCommand(Email: "x@x.co", FullName: "X", Phone: null, Role: role, PreferredLanguage: "vi");
        var r = new CreateAdminUserCommandValidator().Validate(cmd);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorCode == "INVALID_VALUE");
    }

    [Fact]
    public void UpdateUser_InvalidStatus_Fails()
    {
        var cmd = new UpdateUserCommand(Guid.NewGuid(), FullName: null, Phone: null, Status: "frozen", PreferredLanguage: null);
        new UpdateUserCommandValidator().Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateUser_AllNullOptional_Passes()
    {
        var cmd = new UpdateUserCommand(Guid.NewGuid(), FullName: null, Phone: null, Status: null, PreferredLanguage: null);
        new UpdateUserCommandValidator().Validate(cmd).IsValid.Should().BeTrue();
    }
}
