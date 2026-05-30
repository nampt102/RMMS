using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rmms.Application.Common.Abstractions;
using Rmms.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Rmms.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API over throwaway PostgreSQL (PostGIS image — the M01 migration enables the
/// <c>postgis</c> extension) + Redis containers via Testcontainers, applies EF migrations, and
/// swaps the email sender for an in-memory capturing double.
/// </summary>
public sealed class RmmsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.4")
        .WithDatabase("rmms")
        .WithUsername("rmms")
        .WithPassword("rmms_test_pw")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7")
        .Build();

    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            // NOTE: do NOT override Jwt:SigningKey here. Program.cs reads the signing key
            // EAGERLY at top-level (before this in-memory source is appended), so validation
            // would use appsettings.json's key while token issuance (JwtOptions, resolved at
            // runtime) would use the override → signature mismatch → 401. Letting both sides
            // use the appsettings.json key (≥32 bytes) keeps issue/validate consistent.
            // Connection strings / Email are read at runtime, so overriding them is safe.
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString() + ",abortConnect=false",
                ["Email:Provider"] = "Console",
                ["App:AppBaseUrl"] = "http://localhost",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Build the host and apply migrations against the fresh database.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

/// <summary>Shares the (expensive) container fixture across all integration test classes.</summary>
[CollectionDefinition(Name)]
public sealed class RmmsApiCollectionDefinition : ICollectionFixture<RmmsApiFactory>
{
    public const string Name = "rmms-api";
}
