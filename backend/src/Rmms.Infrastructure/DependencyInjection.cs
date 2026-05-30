using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Email;
using Rmms.Infrastructure.Audit;
using Rmms.Infrastructure.Email;
using Rmms.Infrastructure.Identity;
using Rmms.Infrastructure.Persistence;
using Rmms.Infrastructure.Persistence.Interceptors;
using Rmms.Infrastructure.Services;
using StackExchange.Redis;

namespace Rmms.Infrastructure;

/// <summary>
/// Registers Infrastructure services: EF Core (PostgreSQL), Redis, identity (BCrypt + JWT),
/// email, audit logging.
/// External-integration clients (Face / FCM / MinIO) wired in later sprints.
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
                    npg.UseNetTopologySuite();           // GPS / spatial (M05+)
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
        services.AddSingleton<ILoginRateLimiter, Services.RedisLoginRateLimiter>();

        // ----- M01 Identity (Sprint 01) -----
        services.Configure<Rmms.Application.Common.Options.JwtOptions>(
            config.GetSection(Rmms.Application.Common.Options.JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

        // ----- Email -----
        // Sprint 01: console-only. Sprint 01 Day 8 will branch to SendGrid based on Email:Provider config.
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<AppUrlOptions>(config.GetSection(AppUrlOptions.SectionName));
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

        var emailProvider = config.GetSection("Email")["Provider"]?.ToLowerInvariant() ?? "console";
        switch (emailProvider)
        {
            case "sendgrid":
                // TODO(Sprint01-Day8): wire SendGridEmailSender once API key + template IDs ready.
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
                break;
            case "console":
            default:
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
                break;
        }

        // ----- Audit (CR-1) -----
        services.AddScoped<IAuditLogger, DbAuditLogger>();

        // ----- External integrations (deferred) -----
        // TODO(M06): wire FPT.AI Face client via Refit + Polly circuit breaker.
        // TODO(M14): wire Firebase Admin SDK for FCM push.
        // TODO(M13): wire MinIO client for document storage.

        return services;
    }
}
