using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Rmms.IntegrationTests.Infrastructure;
using Xunit;

namespace Rmms.IntegrationTests;

/// <summary>
/// End-to-end M01 auth happy path over the real HTTP + JWT + Postgres + Redis pipeline:
/// register → verify-email → login → /me → refresh (rotation) → logout.
/// </summary>
[Collection(RmmsApiCollectionDefinition.Name)]
public sealed class AuthFlowTests
{
    private readonly RmmsApiFactory _factory;

    public AuthFlowTests(RmmsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task FullAuthFlow_Succeeds()
    {
        using var client = _factory.CreateClient();
        var email = $"pg.{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd1";

        // 1) Register -> 201 pending_email_verify
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Integration PG",
            phone = (string?)null,
            preferredLanguage = "vi",
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2) Verify email using the token captured from the outgoing email
        var verifyToken = _factory.Emails.LatestTokenFor(email);
        verifyToken.Should().NotBeNullOrEmpty("register should send a verification email containing a token");

        var verify = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verifyToken });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2b) A PG MUST supply device info (BR-105). A device-less login is rejected.
        var noDevice = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        noDevice.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3) Login -> 200 with access + refresh tokens
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = ApiHelpers.Device(),
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var (accessToken, refreshToken) = await ReadTokens(login);
        accessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();

        // 4) /auth/me reflects the logged-in user
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var me = await client.GetAsync("/api/v1/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiHelpers.ExtractAsync(me, "email")).Should().Be(email);

        // 5) Refresh rotates the refresh token
        client.DefaultRequestHeaders.Authorization = null;
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, newRefresh) = await ReadTokens(refresh);
        newRefresh.Should().NotBeNullOrEmpty().And.NotBe(refreshToken);

        // 6) Logout is accepted and idempotent
        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = newRefresh });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var logoutAgain = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = newRefresh });
        logoutAgain.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "does-not-exist@example.com",
            password = "WrongPwd1",
            device = ApiHelpers.Device(),
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<(string? Access, string? Refresh)> ReadTokens(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var access = data.TryGetProperty("accessToken", out var a) ? a.GetString() : null;
        var refresh = data.TryGetProperty("refreshToken", out var r) ? r.GetString() : null;
        return (access, refresh);
    }
}
