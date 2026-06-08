using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Rmms.Api.Common;

/// <summary>
/// Parses numeric multipart form fields with invariant culture.
/// Default request culture (vi-VN) treats '.' as a thousands separator, which corrupts
/// GPS coordinates sent as "10.73..." from mobile clients.
/// </summary>
internal static class InvariantFormParsing
{
    public static bool TryParseDouble(IFormCollection form, string field, out double value)
    {
        value = 0;
        if (!form.TryGetValue(field, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        return double.TryParse(
            raw.ToString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static bool TryParseNullableDouble(IFormCollection form, string field, out double? value)
    {
        value = null;
        if (!form.TryGetValue(field, out var raw) || string.IsNullOrWhiteSpace(raw))
            return true;

        if (double.TryParse(
                raw.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
