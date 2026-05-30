using System.Net.Http.Json;
using System.Text.Json;

namespace Rmms.IntegrationTests.Infrastructure;

public static class ApiHelpers
{
    public static object Device(string deviceId = "it-device-1") => new
    {
        deviceId,
        deviceName = "Integration Device",
        os = "ios",
        osVersion = "17.0",
        appVersion = "1.0.0",
        fcmToken = "it-fcm",
    };

    /// <summary>Log in and return the access token, or null if login did not return 200.</summary>
    public static async Task<string?> LoginAsync(this HttpClient client, string email, string password, string deviceId = "it-device-1")
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            device = Device(deviceId),
        });

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await ExtractAsync(resp, "accessToken");
    }

    /// <summary>Read a string field nested under the <c>data</c> envelope.</summary>
    public static async Task<string?> ExtractAsync(HttpResponseMessage resp, string field)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data))
        {
            return null;
        }

        return data.TryGetProperty(field, out var v) ? v.GetString() : null;
    }
}
