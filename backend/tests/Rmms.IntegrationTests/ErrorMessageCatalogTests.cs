using FluentAssertions;
using Rmms.Api.Localization;
using Rmms.Shared.Errors;
using Xunit;

namespace Rmms.IntegrationTests;

/// <summary>
/// Pure unit test (no container / not in the API collection) for the vi/en error catalog.
/// </summary>
public sealed class ErrorMessageCatalogTests
{
    private readonly ErrorMessageCatalog _catalog = new();

    [Fact]
    public void Vietnamese_IsDefaultLanguage()
    {
        _catalog.Localize(ErrorCodes.InvalidCredentials, "vi").Should().Be("Email hoặc mật khẩu không đúng.");
    }

    [Fact]
    public void English_IsReturnedForEnCulture()
    {
        _catalog.Localize(ErrorCodes.InvalidCredentials, "en").Should().Be("Wrong email or password.");
    }

    [Fact]
    public void RegionalCulture_MapsToBaseLanguage()
    {
        _catalog.Localize(ErrorCodes.DeviceNotAuthorized, "en-US").Should().StartWith("This device");
    }

    [Fact]
    public void UnknownCode_ReturnsNull()
    {
        _catalog.Localize("NOT_A_REAL_CODE", "vi").Should().BeNull();
    }

    [Fact]
    public void UnknownCulture_FallsBackToVietnamese()
    {
        _catalog.Localize(ErrorCodes.AccountInactive, "fr").Should().StartWith("Tài khoản");
    }
}
