using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rmms.Application;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Infrastructure;
using Rmms.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ----- Serilog -----
builder.Services.AddSerilog(lc => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

// ----- Layers -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Worker has no HTTP context; provide a null current-user (jobs run as system).
builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();

// ----- Hangfire server -----
var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(postgres)));

builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = Math.Max(2, Environment.ProcessorCount);
    opts.Queues = new[] { "default", "critical", "low" };
});

var host = builder.Build();

// ----- Recurring jobs -----
// Auth token cleanup (M01 Sprint 01 Day 6): hourly hard-delete of spent/expired tokens.
// Use IRecurringJobManager (resolved from DI) rather than the static RecurringJob facade so
// it binds to the configured storage without relying on JobStorage.Current init timing.
using (var scope = host.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<ITokenCleanupService>(
        "auth-token-cleanup",
        "low",
        svc => svc.RunAsync(CancellationToken.None),
        Cron.Hourly());

    // Attendance photo retention (M05, CR-4): daily purge of selfies/store photos > 90 days.
    recurringJobs.AddOrUpdate<IAttendancePhotoRetentionService>(
        "attendance-photo-retention",
        "low",
        svc => svc.RunAsync(CancellationToken.None),
        Cron.Daily());
}

await host.RunAsync();
