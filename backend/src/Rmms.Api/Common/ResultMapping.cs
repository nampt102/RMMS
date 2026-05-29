using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Api.Common;

/// <summary>
/// Maps domain <see cref="Result{T}"/> to ASP.NET <see cref="IActionResult"/>
/// + the structured <see cref="ErrorEnvelope"/> from <c>05-api-conventions.md</c>.
///
/// Centralizing this here keeps controllers thin and HTTP-status / payload
/// consistent across endpoints.
/// </summary>
internal static class ResultMapping
{
    /// <summary>Wrap a success value as <c>{ "data": ... }</c>.</summary>
    public static IActionResult Ok<T>(T value) =>
        new OkObjectResult(new { data = value });

    /// <summary>Wrap a success value as a 201 Created.</summary>
    public static IActionResult Created<T>(T value) =>
        new ObjectResult(new { data = value })
        {
            StatusCode = StatusCodes.Status201Created,
        };

    /// <summary>Translate failure to the right HTTP status + ErrorEnvelope.</summary>
    public static IActionResult Failure(Error error, string? traceId)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        var envelope = new ErrorEnvelope(new ErrorBody(
            Code: string.IsNullOrEmpty(error.Code) ? ErrorCodes.InternalError : error.Code,
            Message: error.Message,
            Details: null,
            TraceId: traceId));

        return new ObjectResult(envelope) { StatusCode = status };
    }

    /// <summary>Convenience for handlers returning <see cref="Result{T}"/>.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, HttpContext httpContext, int successStatusCode = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? (successStatusCode == StatusCodes.Status201Created ? Created(result.Value) : Ok(result.Value))
            : Failure(result.Error, httpContext.TraceIdentifier);
}
