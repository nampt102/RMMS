using System.Net;
using System.Text.Json;
using FluentValidation;
using Rmms.Shared.Errors;

namespace Rmms.Api.Middlewares;

/// <summary>
/// Converts thrown exceptions into the structured <see cref="ErrorEnvelope"/>
/// per <c>knowledge-base/05-api-conventions.md</c>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, HttpStatusCode.UnprocessableEntity,
                new ErrorBody(
                    Code: ErrorCodes.ValidationFailed,
                    Message: "Dữ liệu không hợp lệ.",
                    Details: ex.Errors.Select(e => new ErrorDetail(
                        Field: e.PropertyName,
                        Code: e.ErrorCode ?? "INVALID",
                        Message: e.ErrorMessage)).ToList(),
                    TraceId: context.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteAsync(context, HttpStatusCode.Unauthorized,
                new ErrorBody(ErrorCodes.TokenInvalid, "Bạn cần đăng nhập.", null, context.TraceIdentifier));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Not found: {Message}", ex.Message);
            await WriteAsync(context, HttpStatusCode.NotFound,
                new ErrorBody(ErrorCodes.NotFound, ex.Message, null, context.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in pipeline.");
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                new ErrorBody(
                    Code: ErrorCodes.InternalError,
                    Message: _env.IsDevelopment() ? ex.Message : "Đã xảy ra lỗi không mong muốn.",
                    Details: null,
                    TraceId: context.TraceIdentifier));
        }
    }

    private static async Task WriteAsync(HttpContext ctx, HttpStatusCode status, ErrorBody body)
    {
        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, new ErrorEnvelope(body), JsonOpts);
    }
}
