using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rmms.Application.Common.Interfaces;
using Rmms.Infrastructure.Persistence;
using Rmms.Infrastructure.Persistence.Interceptors;
using Rmms.Infrastructure.Services;
using StackExchange.Redis;

namespace Rmms.Infrastructure;

/// <summary>
/// Registers Infrastructure services: EF Core (PostgreSQL), Redis, Hangfire storage,
/// external API clients (Face / FCM / SendGrid / MinIO).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ----- Clock / Auditing -----
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        // ----- EF Core + PostgreSQL -----
        var connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres in configuration.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(connectionString, npg =>
                {
                    npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npg.UseNetTopologySuite();           // GPS / spatial
                    npg.EnableRetryOnFailure(3);
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ----- Redis (shared connection multiplexer) -----
        var redisConn = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis in configuration.");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);

        // ----- External integrations registered later (Face, FCM, SendGrid, MinIO) -----
        // TODO(M01+): wire FPT.AI Face client via Refit + Polly circuit breaker.
        // TODO(M01+): wire SendGrid client.
        // TODO(M01+): wire Firebase Admin SDK.
        // TODO(M01+): wire MinIO client.

        return services;
    }
}
