using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rmms.Infrastructure.Persistence;
using Rmms.IntegrationTests.Infrastructure;
using Rmms.Shared.Errors;
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
    public async Task RefreshTokenReuse_RevokesAllSessions_Returns401()
    {
        using var client = _factory.CreateClient();
        var email = $"pg.{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd1";

        // Register -> verify -> login to obtain a usable refresh token (A).
        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Reuse PG",
            phone = (string?)null,
            preferredLanguage = "vi",
        });
        var verifyToken = _factory.Emails.LatestTokenFor(email);
        await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verifyToken });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = ApiHelpers.Device(),
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, refreshA) = await ReadTokens(login);

        // Rotate once: A -> B. A is now revoked (rotated).
        var rotate = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refreshA });
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, refreshB) = await ReadTokens(rotate);
        refreshB.Should().NotBeNullOrEmpty().And.NotBe(refreshA);

        // Replay the already-rotated token A -> reuse detected -> 401 + REFRESH_TOKEN_REUSED.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refreshA });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadErrorCode(reuse)).Should().Be(ErrorCodes.RefreshTokenReused);

        // Reuse nukes ALL active sessions, so the freshly issued B must also be dead now.
        var afterNuke = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refreshB });
        afterNuke.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongCredentials_LocalizesMessage_ByAcceptLanguage()
    {
        using var client = _factory.CreateClient();

        var en = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com", password = "WrongPwd1", device = ApiHelpers.Device() }),
        };
        en.Headers.Add("Accept-Language", "en");
        var enResp = await client.SendAsync(en);
        enResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadErrorMessage(enResp)).Should().Be("Wrong email or password.");

        var vi = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com", password = "WrongPwd1", device = ApiHelpers.Device() }),
        };
        vi.Headers.Add("Accept-Language", "vi");
        var viResp = await client.SendAsync(vi);
        (await ReadErrorMessage(viResp)).Should().Be("Email hoặc mật khẩu không đúng.");
    }

    [Fact]
    public async Task AuthFlow_EmitsCr1AuditEntries()
    {
        using var client = _factory.CreateClient();
        var email = $"pg.{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd1";

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Audit PG",
            phone = (string?)null,
            preferredLanguage = "vi",
        });
        var verifyToken = _factory.Emails.LatestTokenFor(email);
        await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verifyToken });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, device = ApiHelpers.Device() });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var actions = await db.AuditLogs
            .Where(a => a.TargetId == user.Id)
            .Select(a => a.Action)
            .ToListAsync();

        actions.Should().Contain("user.registered")
            .And.Contain("user.email_verified")
            .And.Contain("auth.login_success");
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

    private static async Task<string?> ReadErrorCode(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("error", out var error)
            && error.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
    }

    private static async Task<string?> ReadErrorMessage(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("error", out var error)
            && error.TryGetProperty("message", out var message)
            ? message.GetString()
            : null;
    }
}
