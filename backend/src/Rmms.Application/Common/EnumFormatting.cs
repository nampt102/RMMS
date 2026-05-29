using System.Text;

namespace Rmms.Application.Common;

/// <summary>
/// Format helpers for converting enums to API-friendly strings.
///
/// Spec: <c>knowledge-base/05-api-conventions.md</c> §"Enums" — "lowercase string values".
/// We additionally use snake_case to match the DB column values
/// (kept in sync via EF Core converters in <c>UserConfiguration</c> /
/// <c>UserDeviceConfiguration</c>).
///
/// Example: <c>UserStatus.PendingEmailVerify</c> → <c>"pending_email_verify"</c>.
/// </summary>
public static class EnumFormatting
{
    /// <summary>Convert a PascalCase enum value to lowercase snake_case for API/DB consistency.</summary>
    public static string ToSnakeCase<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var raw = value.ToString();
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new StringBuilder(raw.Length + 4);
        for (var i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]))
            {
                sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(raw[i]));
        }
        return sb.ToString();
    }
}
