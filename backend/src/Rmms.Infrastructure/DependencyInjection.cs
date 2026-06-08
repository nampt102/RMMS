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
using Minio;
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
                services.AddScoped<IEmailSender, SendGridEmailSender>();
                break;
            case "smtp":
                services.AddScoped<IEmailSender, SmtpEmailSender>();
                break;
            case "console":
            default:
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
                break;
        }

        // ----- M09 Approval Workflow -----
        services.Configure<Rmms.Application.Common.Options.ApprovalOptions>(
            config.GetSection(Rmms.Application.Common.Options.ApprovalOptions.SectionName));
        services.AddSingleton<IApprovalTokenService, Approvals.ApprovalTokenService>();
        services.AddScoped<IApprovalService, Approvals.ApprovalService>();

        // ----- M14 Notification (basic: in-app + push + email) -----
        // Push provider: logging by default (Phase 1A, no Firebase credential needed);
        // swap to a real FCM HTTP v1 sender in Phase 1B via Push:Provider=fcm.
        var pushProvider = config.GetSection("Push")["Provider"]?.ToLowerInvariant() ?? "console";
        switch (pushProvider)
        {
            // case "fcm": services.AddScoped<IPushSender, Notifications.FcmPushSender>(); break; // 1B
            default:
                services.AddScoped<IPushSender, Notifications.LoggingPushSender>();
                break;
        }
        services.AddScoped<INotificationService, Notifications.NotificationService>();

        // ----- Audit (CR-1) -----
        services.AddScoped<IAuditLogger, DbAuditLogger>();

        // ----- M06 Face Verification (ADR-011: self-hosted CompreFace) -----
        // Real CompreFace client when an API key is configured, else a deterministic dev client.
        services.Configure<Rmms.Application.Common.Options.CompreFaceOptions>(
            config.GetSection(Rmms.Application.Common.Options.CompreFaceOptions.SectionName));
        var compreFace = config.GetSection(Rmms.Application.Common.Options.CompreFaceOptions.SectionName);
        var compreFaceKey = compreFace["ApiKey"];
        if (!string.IsNullOrWhiteSpace(compreFaceKey))
        {
            var baseUrl = (compreFace["BaseUrl"] ?? "http://compreface-fe:8080").TrimEnd('/') + "/";
            services.AddHttpClient<IFaceClient, Face.CompreFaceClient>(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                c.DefaultRequestHeaders.Add("x-api-key", compreFaceKey);
                c.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddScoped<IFaceClient, Face.DevFaceClient>();
        }
        services.AddScoped<IFaceVerificationService, Face.FaceVerificationService>();

        // Photo storage: MinIO when an endpoint is configured, else a no-op local fallback.
        services.Configure<Rmms.Application.Common.Options.MinioOptions>(
            config.GetSection(Rmms.Application.Common.Options.MinioOptions.SectionName));
        var minioEndpoint = config.GetSection(Rmms.Application.Common.Options.MinioOptions.SectionName)["Endpoint"];
        if (!string.IsNullOrWhiteSpace(minioEndpoint))
        {
            var minio = config.GetSection(Rmms.Application.Common.Options.MinioOptions.SectionName);
            services.AddSingleton<Minio.IMinioClient>(_ => new Minio.MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minio["AccessKey"], minio["SecretKey"])
                .WithSSL(bool.TryParse(minio["UseSsl"], out var ssl) && ssl)
                .Build());
            services.AddScoped<IAttendancePhotoStorage, Attendance.MinioAttendancePhotoStorage>();
        }
        else
        {
            services.AddScoped<IAttendancePhotoStorage, Attendance.LocalAttendancePhotoStorage>();
        }

        // ----- External integrations (deferred) -----
        // TODO(M06): replace StubFaceVerificationService with FPT.AI Face client (Refit + Polly).
        // TODO(M14): wire Firebase Admin SDK for FCM push.

        return services;
    }
}
