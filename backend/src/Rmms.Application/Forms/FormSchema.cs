using System.Text.Json;

namespace Rmms.Application.Forms;

/// <summary>
/// Server-side validator for the Form Engine JSONB schema (ADR-016 + M10 design doc).
/// Validates the schema STRUCTURE against the input-type registry — NOT a generic JSON-Schema
/// check — so field references and the known type set are enforced. Submission-value validation
/// (per-field rules, visible_if) lands with the submit endpoint in a later sprint.
/// </summary>
public static class FormSchema
{
    /// <summary>The 12+ input types the Builder/renderer/validator agree on (design doc §2.2).</summary>
    public static readonly IReadOnlySet<string> KnownFieldTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "text", "number", "single_choice", "multi_choice", "dropdown", "datetime",
        "image_upload", "camera", "file", "product_selector", "store_selector",
        "brand_sku_selector", "section",
    };

    public sealed record ValidationResult(bool IsValid, string? Error)
    {
        public static readonly ValidationResult Ok = new(true, null);
        public static ValidationResult Fail(string error) => new(false, error);
    }

    /// <summary>
    /// Validate a raw JSON schema string. Requires a top-level object with a <c>fields</c> array;
    /// each field needs a unique non-empty <c>id</c> and a <c>type</c> in <see cref="KnownFieldTypes"/>.
    /// An optional <c>rules</c> member must be an object.
    /// </summary>
    public static ValidationResult Validate(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return ValidationResult.Fail("Schema rỗng.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Fail($"Schema không phải JSON hợp lệ: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Fail("Schema phải là một JSON object.");
            }

            if (!root.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Fail("Schema phải có mảng 'fields'.");
            }

            if (root.TryGetProperty("rules", out var rules) && rules.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Fail("'rules' phải là một object nếu có.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var field in fields.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.Object)
                {
                    return ValidationResult.Fail($"fields[{index}] phải là object.");
                }

                if (!field.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(id.GetString()))
                {
                    return ValidationResult.Fail($"fields[{index}] thiếu 'id'.");
                }

                var idValue = id.GetString()!;
                if (!ids.Add(idValue))
                {
                    return ValidationResult.Fail($"Trùng field id '{idValue}'.");
                }

                if (!field.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String
                    || !KnownFieldTypes.Contains(type.GetString() ?? string.Empty))
                {
                    return ValidationResult.Fail($"fields[{index}] ('{idValue}') có 'type' không hợp lệ.");
                }

                index++;
            }

            return ValidationResult.Ok;
        }
    }
}
