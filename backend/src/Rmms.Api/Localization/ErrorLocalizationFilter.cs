using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rmms.Shared.Errors;

namespace Rmms.Api.Localization;

/// <summary>
/// Localizes the <see cref="ErrorEnvelope"/> message by error code for the active request culture
/// (set by <c>UseRequestLocalization</c> from the <c>Accept-Language</c> header; default vi).
///
/// Centralizing here means handlers keep returning a single (Vietnamese) default message as a
/// fallback, and the API still emits the correct vi/en copy without per-controller changes.
/// Unknown codes are left untouched.
/// </summary>
public sealed class ErrorLocalizationFilter : IActionFilter
{
    private readonly IErrorMessageLocalizer _localizer;

    public ErrorLocalizationFilter(IErrorMessageLocalizer localizer) => _localizer = localizer;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No-op: we localize on the way out.
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult { Value: ErrorEnvelope env } result)
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localized = _localizer.Localize(env.Error.Code, culture);
            if (localized is not null)
            {
                result.Value = new ErrorEnvelope(env.Error with { Message = localized });
            }
        }
    }
}
