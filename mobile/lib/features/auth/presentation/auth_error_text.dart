import '../../../core/network/api_exception.dart';
import '../../../l10n/generated/app_localizations.dart';

/// Maps an [ApiException] to a localized, user-facing message.
///
/// The backend already returns a localized `message` honoring `Accept-Language`,
/// but we prefer client strings for the codes the UI cares about so copy stays
/// consistent and testable; we fall back to the server message otherwise.
String authErrorText(AppLocalizations l, ApiException e) {
  switch (e.code) {
    case ApiErrorCodes.invalidCredentials:
      return l.errorInvalidCredentials;
    case ApiErrorCodes.emailNotVerified:
      return l.errorEmailNotVerified;
    case ApiErrorCodes.accountInactive:
      return l.errorAccountInactive;
    case ApiErrorCodes.accountLocked:
      return l.errorAccountLocked;
    case ApiErrorCodes.emailAlreadyRegistered:
      return l.errorEmailAlreadyRegistered;
    case ApiErrorCodes.passwordTooWeak:
      return l.errorPasswordTooWeak;
    case ApiErrorCodes.emailTokenExpired:
      return l.errorEmailTokenExpired;
    case ApiErrorCodes.emailTokenUsed:
      return l.errorEmailTokenUsed;
    case ApiErrorCodes.rateLimitExceeded:
      return l.errorRateLimited;
    case ApiErrorCodes.network:
      return l.errorNetwork;
    case ApiErrorCodes.tokenExpired:
    case ApiErrorCodes.tokenInvalid:
      return l.errorTokenExpired;
    default:
      // Prefer the server's localized message; fall back to a generic string.
      return e.message.isNotEmpty ? e.message : l.errorGeneric;
  }
}
