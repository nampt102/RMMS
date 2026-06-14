using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Forms;

/// <summary>
/// A dynamic form definition (M10 + <c>04-data-model.md</c> forms). The <see cref="Form"/> is the
/// stable identity (unique <see cref="Code"/>); the actual fields/rules live in immutable
/// <see cref="FormVersion"/> rows. Editing a published form creates a NEW version (BR-505) — old
/// versions stay so existing submissions keep resolving (AC-21).
/// </summary>
public sealed class Form : AuditableEntity, IAggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string NameVi { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string? DescriptionVi { get; private set; }
    public string? DescriptionEn { get; private set; }
    public FormType FormType { get; private set; }

    /// <summary>Latest PUBLISHED version number; 0 until the first publish.</summary>
    public int CurrentVersion { get; private set; }

    public FormStatus Status { get; private set; }

    private Form() { } // EF Core

    public static Form Create(
        string code, string nameVi, string nameEn, string? descriptionVi, string? descriptionEn, FormType formType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameVi);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameEn);

        return new Form
        {
            Code = code.Trim(),
            NameVi = nameVi.Trim(),
            NameEn = nameEn.Trim(),
            DescriptionVi = string.IsNullOrWhiteSpace(descriptionVi) ? null : descriptionVi.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim(),
            FormType = formType,
            CurrentVersion = 0,
            Status = FormStatus.Draft,
        };
    }

    /// <summary>Update the form-level metadata (does not touch the schema/version).</summary>
    public void UpdateMeta(string nameVi, string nameEn, string? descriptionVi, string? descriptionEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameVi);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameEn);

        NameVi = nameVi.Trim();
        NameEn = nameEn.Trim();
        DescriptionVi = string.IsNullOrWhiteSpace(descriptionVi) ? null : descriptionVi.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
    }

    /// <summary>Mark <paramref name="version"/> as the active published version (BR-505).</summary>
    public void MarkPublished(int version)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        CurrentVersion = version;
        Status = FormStatus.Published;
    }

    public void Archive() => Status = FormStatus.Archived;
}
