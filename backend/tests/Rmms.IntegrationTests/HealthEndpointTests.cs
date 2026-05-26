using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Rmms.IntegrationTests;

/// <summary>
/// Smoke test — proves the scaffold spins up and serves the trivial /api/v1/health endpoint.
/// More integration tests come per-module starting Sprint 01.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact(Skip = "Requires real Postgres+Redis. Enable once Testcontainers fixture is wired in Sprint 00.")]
    public async Task GetHealth_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
