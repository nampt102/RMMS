using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Rmms.Infrastructure.Logging;

/// <summary>
/// Adds an Elasticsearch sink to Serilog when an <c>Elasticsearch</c> config section is present, so
/// both the API and the Worker ship structured logs to a central ES cluster (02-tech-stack.md).
///
/// Config (per host appsettings / env — secrets stay out of source control):
/// <code>
/// "Elasticsearch": {
///   "Url": "https://servermm:9200",
///   "Username": "",            // optional — basic auth (used only when ApiKey is empty)
///   "Password": "",
///   "ApiKey": "base64(id:key)",// preferred — sent as "Authorization: ApiKey ..."
///   "IndexFormat": "rmms-api-{0:yyyy.MM.dd}",   // {0} = log timestamp; daily rollover
///   "AllowInvalidCertificates": false           // set true only for internal self-signed TLS
/// }
/// </code>
/// When <c>Url</c> is empty the call is a no-op, so dev keeps console/file logging only.
/// </summary>
public static class SerilogElasticsearchExtensions
{
    public static LoggerConfiguration WriteToElasticsearchIfConfigured(
        this LoggerConfiguration loggerConfiguration, IConfiguration config, string component)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(config);

        var section = config.GetSection("Elasticsearch");
        var url = section["Url"];
        if (string.IsNullOrWhiteSpace(url))
        {
            return loggerConfiguration; // not configured → leave console/file sinks untouched
        }

        var apiKey = section["ApiKey"];
        var username = section["Username"];
        var password = section["Password"];
        var allowInvalidCerts = bool.TryParse(section["AllowInvalidCertificates"], out var allow) && allow;

        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        var indexFormat = section["IndexFormat"];
        if (string.IsNullOrWhiteSpace(indexFormat))
        {
            // Suggested default: one daily index per component+environment, e.g. rmms-api-production-2026.06.17.
            indexFormat = $"rmms-{component}-{environment.ToLowerInvariant()}-{{0:yyyy.MM.dd}}";
        }

        var options = new ElasticsearchSinkOptions(new Uri(url))
        {
            IndexFormat = indexFormat,
            // Let ES create the field mapping on first write — avoids coupling to a template version.
            AutoRegisterTemplate = false,
            // Skip the version pre-flight so the sink talks to ES 8.x without a product check.
            DetectElasticsearchVersion = false,
            // Never let a logging failure throw into the request/job path; write to Serilog self-log.
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
            ModifyConnectionSettings = conn =>
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    conn.GlobalHeaders(new NameValueCollection { { "Authorization", $"ApiKey {apiKey}" } });
                }
                else if (!string.IsNullOrWhiteSpace(username))
                {
                    conn.BasicAuthentication(username, password ?? string.Empty);
                }

                if (allowInvalidCerts)
                {
                    conn.ServerCertificateValidationCallback((_, _, _, _) => true);
                }

                return conn;
            },
        };

        return loggerConfiguration.WriteTo.Elasticsearch(options);
    }
}
