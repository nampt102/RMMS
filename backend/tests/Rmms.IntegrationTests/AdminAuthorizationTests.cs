using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Rmms.Domain.Enums;
using Rmms.IntegrationTests.Infrastructure;
using Xunit;

namespace Rmms.IntegrationTests;

/// <summary>
/// Regression coverage for the JWT role-authorization bug fixed on 2026-05-31:
/// <c>[Authorize(Policy = AdminOnly)]</c> must allow <c>admin</c> tokens (200) and reject
/// every other role (403). This exercises the full HTTP + JWT pipeline that handler unit
/// tests bypass — the gap that let the original <c>MapInboundClaims</c> bug slip through.
/// </summary>
[Collection(RmmsApiCollectionDefinition.Name)]
public sealed class AdminAuthorizationTests
{
    private readonly RmmsApiFactory _factory;

    public AdminAuthorizationTests(RmmsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminToken_CanListUsers_200()
    {
        var admin = await _factory.SeedUserAsync(UserRole.Admin);
        using var client = _factory.CreateClient();

        var token = await client.LoginAsync(admin.Email, admin.Password, "admin-device");
        token.Should().NotBeNullOrEmpty("admin login should succeed and return an access token");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync("/api/v1/admin/users?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LeaderToken_CannotListUsers_403()
    {
        var leader = await _factory.SeedUserAsync(UserRole.Leader);
        using var client = _factory.CreateClient();

        var token = await client.LoginAsync(leader.Email, leader.Password, "leader-device");
        token.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync("/api/v1/admin/users");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoToken_IsUnauthorized_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/admin/users");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
