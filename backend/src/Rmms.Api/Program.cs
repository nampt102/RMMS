using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Rmms.Api.Authentication;
using Rmms.Api.Middlewares;
using Rmms.Application;
using Rmms.Application.Common.Interfaces;
using Rmms.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----- Serilog (structured logs per 02-tech-stack.md) -----
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId());

// ----- Layer registrations -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ----- HttpContext + CurrentUser + ClientContext -----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<Rmms.Application.Common.Abstractions.IClientContext, HttpContextClientContext>();

// ----- AuthN (JWT) per 05-api-conventions.md -----
// Note: Infrastructure layer binds `JwtOptions` from the same `Jwt` section
// for issuance (see Rmms.Infrastructure.Identity.JwtTokenService).
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey.");
if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes for HS256.");
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT claim names EXACTLY as issued ("role", "sub", "email").
        // With the default (true), the handler remaps short claim names (e.g.
        // "sub" -> ClaimTypes.NameIdentifier) which desyncs the explicit
        // RoleClaimType="role" / NameClaimType="sub" set below and makes
        // [Authorize(Roles="admin")] return 403 even for a valid admin token.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            // Map our custom "role" claim → ClaimTypes.Role so [Authorize(Roles="admin")] works.
            // Our JWT emits role as lowercase string ("admin", "leader", "buh", "pg") — match exactly.
            RoleClaimType = "role",
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddRmmsAuthorization();

// ----- CORS -----
const string CorsPolicy = "RmmsDefault";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// ----- i18n (vi default, en supported) per 05-api-conventions.md "Accept-Language" -----
builder.Services.AddLocalization();
var supportedCultures = new[] { "vi", "en" }
    .Select(c => new System.Globalization.CultureInfo(c)).ToArray();
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("vi");
    o.SupportedCultures = supportedCultures;
    o.SupportedUICultures = supportedCultures;
});

// ----- Error message localization (Day 8): vi/en by error code, applied via action filter -----
builder.Services.AddSingleton<Rmms.Api.Localization.IErrorMessageLocalizer, Rmms.Api.Localization.ErrorMessageCatalog>();
builder.Services.AddScoped<Rmms.Api.Localization.ErrorLocalizationFilter>();

// ----- Controllers + JSON options -----
builder.Services.AddControllers(options => options.Filters.Add<Rmms.Api.Localization.ErrorLocalizationFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ----- OpenAPI -----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "RMMS API", Version = "v1" });
    var bearer = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header — Bearer {token}",
    };
    o.AddSecurityDefinition("Bearer", bearer);
    o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer",
            },
        }] = Array.Empty<string>(),
    });
});

// ----- Health checks -----
builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== CLI commands (run BEFORE HTTP pipeline) =====
// Pattern: `dotnet run -- seed-admin --email=... --password=...`
// EF Core design-time tools also pass `args` here; ignore unknown commands silently
// so `dotnet ef migrations add` continues to work.
if (args.Length > 0)
{
    var firstArg = args[0];
    if (firstArg.Equals("seed-admin", StringComparison.OrdinalIgnoreCase))
    {
        return await Rmms.Api.Cli.SeedAdminCommand.RunAsync(args, app.Services);
    }
}

// ===== Middleware pipeline =====
app.UseSerilogRequestLogging(opts =>
{
    // Day 8: enrich the per-request completion log with identity + correlation properties.
    opts.EnrichDiagnosticContext = (diagnostic, httpContext) =>
    {
        diagnostic.Set("TraceId", httpContext.TraceIdentifier);
        var currentUser = httpContext.RequestServices.GetService<Rmms.Application.Common.Interfaces.ICurrentUser>();
        if (currentUser?.UserId is { } userId)
        {
            diagnostic.Set("UserId", userId);
        }
        if (currentUser?.Role is { } role)
        {
            diagnostic.Set("Role", role.ToString().ToLowerInvariant());
        }
        if (currentUser?.DeviceId is { } deviceId && deviceId != Guid.Empty)
        {
            diagnostic.Set("DeviceId", deviceId);
        }
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// Day 8: push TraceId/UserId/DeviceId/Role into the Serilog LogContext for all handler logs
// (runs after auth so JWT claims are resolved).
app.UseMiddleware<Rmms.Api.Logging.RequestEnrichmentMiddleware>();

// Idempotent replay for mutations carrying X-Idempotency-Key (scoped per authenticated user).
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

await app.RunAsync();
return 0;

// Marker for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
