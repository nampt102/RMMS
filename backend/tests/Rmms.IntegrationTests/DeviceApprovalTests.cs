using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Rmms.Domain.Enums;
using Rmms.IntegrationTests.Infrastructure;
using Xunit;

namespace Rmms.IntegrationTests;

/// <summary>
/// AC-3 end-to-end (BR-105/BR-106): a second device is blocked until an Admin approves it,
/// after which it can log in. Exercises the real HTTP + JWT + Postgres pipeline.
/// </summary>
[Collection(RmmsApiCollectionDefinition.Name)]
public sealed class DeviceApprovalTests
{
    private readonly RmmsApiFactory _factory;

    public DeviceApprovalTests(RmmsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SecondDevice_Blocked_ThenApprovedByAdmin_CanLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"pg.{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd1";

        // Register + verify the PG.
        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Device PG",
            phone = (string?)null,
            preferredLanguage = "vi",
        });
        var verifyToken = _factory.Emails.LatestTokenFor(email);
        await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verifyToken });

        // First device logs in fine.
        var first = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = ApiHelpers.Device("device-1"),
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second device is blocked (BR-105).
        var second = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = ApiHelpers.Device("device-2"),
        });
        second.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin lists pending requests and approves device-2.
        var admin = await _factory.SeedUserAsync(UserRole.Admin);
        using var adminClient = _factory.CreateClient();
        var adminToken = await adminClient.LoginNoDeviceAsync(admin.Email, admin.Password);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var pending = await adminClient.GetAsync("/api/v1/devices/pending");
        pending.StatusCode.Should().Be(HttpStatusCode.OK);
        var deviceRowId = await FindPendingDeviceId(pending, email);
        deviceRowId.Should().NotBeNull("the blocked device-2 must appear as a pending request");

        var approve = await adminClient.PostAsync($"/api/v1/devices/{deviceRowId}/approve", null);
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Device-2 can now log in.
        var retry = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = ApiHelpers.Device("device-2"),
        });
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<string?> FindPendingDeviceId(HttpResponseMessage resp, string email)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            if (item.TryGetProperty("userEmail", out var e) && e.GetString() == email)
            {
                return item.GetProperty("deviceId").GetString();
            }
        }

        return null;
    }
}
