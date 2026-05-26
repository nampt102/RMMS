namespace Rmms.Shared.Errors;

/// <summary>
/// API error envelope per <c>knowledge-base/05-api-conventions.md</c>:
/// <code>
/// { "error": { "code": "...", "message": "...", "details": [...], "traceId": "..." } }
/// </code>
/// </summary>
public sealed record ErrorEnvelope(ErrorBody Error);

public sealed record ErrorBody(
    string Code,
    string Message,
    IReadOnlyList<ErrorDetail>? Details,
    string? TraceId);

public sealed record ErrorDetail(string Field, string Code, string Message);
