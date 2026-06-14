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
