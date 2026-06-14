using System.Text.Json;

namespace Rmms.Api.Dtos.Forms;

/// <summary>Admin Form Builder request bodies (M10). Schema is the raw JSONB definition string.</summary>
public sealed record CreateFormRequest(
    string Code,
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string FormType,
    string Schema);

public sealed record UpdateFormRequest(
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Schema);

/// <summary>Add one targeting rule to a form (M10 assignment, OR logic).</summary>
public sealed record AssignFormRequest(
    string? Role,
    Guid? UserId,
    Guid? StoreId,
    Guid? AreaId,
    Guid? CategoryId,
    Guid? ProductId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

/// <summary>Submit a filled form (M10). Answers/Attachments are JSON objects (field id → value).</summary>
public sealed record SubmitFormRequest(
    JsonElement Answers,
    JsonElement? Attachments,
    Guid? StoreId,
    int TimeSpentSeconds,
    string ClientIdempotencyKey);
