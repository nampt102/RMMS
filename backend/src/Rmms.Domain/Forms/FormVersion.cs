using Rmms.Domain.Common;

namespace Rmms.Domain.Forms;

/// <summary>
/// One immutable version of a <see cref="Form"/>'s schema (M10 + <c>04-data-model.md</c>
/// form_versions). <see cref="Schema"/> is the JSONB definition (fields[] + rules{}, see
/// <c>modules/M10-form-engine-design.md</c>). Submissions FK to a specific version so a later
/// edit never mutates historical answers (BR-505/AC-21). A version with <see cref="PublishedAt"/>
/// = null is the editable draft; once published it is frozen.
/// </summary>
public sealed class FormVersion : AuditableEntity, IAggregateRoot
{
    public Guid FormId { get; private set; }
    public int Version { get; private set; }

    /// <summary>Raw JSON schema string (jsonb column).</summary>
    public string Schema { get; private set; } = "{}";

    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }

    public bool IsPublished => PublishedAt is not null;

    private FormVersion() { } // EF Core

    public static FormVersion CreateDraft(Guid formId, int version, string schema)
    {
        if (formId == Guid.Empty) throw new ArgumentException("Form id is required.", nameof(formId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        return new FormVersion
        {
            FormId = formId,
            Version = version,
            Schema = schema,
        };
    }

    /// <summary>Replace the schema. Only allowed while the version is still a draft.</summary>
    public void UpdateSchema(string schema)
    {
        if (IsPublished) throw new InvalidOperationException("Cannot edit a published form version.");
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        Schema = schema;
    }

    public void Publish(DateTimeOffset now, Guid? publishedBy)
    {
        if (IsPublished) return;
        PublishedAt = now;
        PublishedBy = publishedBy;
    }
}
