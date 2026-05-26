using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rmms.Application;
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
await host.RunAsync();
