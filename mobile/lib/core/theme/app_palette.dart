import 'package:flutter/material.dart';

/// Raw design tokens for the RMMS 2026 mobile theme.
///
/// Direction (per ui-ux-pro-max): modern **flat + soft-depth**, vibrant
/// **indigo** brand paired with an **emerald** success/action accent — a trendy,
/// legible system for a field-workforce attendance product. Colours live here as
/// tokens; widgets read them through [ColorScheme] or the [AppSemantics]
/// ThemeExtension, never as ad-hoc hex (see `color-semantic`).
class AppPalette {
  AppPalette._();

  // ---- Brand (indigo) ---------------------------------------------------
  static const Color indigo = Color(0xFF6366F1); // primary
  static const Color indigoDeep = Color(0xFF4F46E5); // gradient end / pressed
  static const Color indigoLight = Color(0xFF818CF8); // secondary / dark primary
  static const Color indigoTintBg = Color(0xFFF6F7FB); // app background (light)

  // ---- Semantic accents -------------------------------------------------
  static const Color emerald = Color(0xFF059669); // success / "checked-in"
  static const Color emeraldLight = Color(0xFF34D399); // dark-mode success
  static const Color amber = Color(0xFFD97706); // warning / pending
  static const Color red = Color(0xFFDC2626); // error / violation
  static const Color sky = Color(0xFF2563EB); // info

  // ---- Neutrals ---------------------------------------------------------
  static const Color ink = Color(0xFF1E1B4B); // headings (deep indigo-ink)
  static const Color body = Color(0xFF334155); // body text
  static const Color muted = Color(0xFF64748B); // secondary text
  static const Color border = Color(0xFFE2E8F0);
  static const Color surfaceMuted = Color(0xFFEEF1F8); // input fill / chips
  static const Color white = Color(0xFFFFFFFF);

  // ---- Dark neutrals ----------------------------------------------------
  static const Color darkBg = Color(0xFF0F1117);
  static const Color darkSurface = Color(0xFF171A21);
  static const Color darkSurfaceHi = Color(0xFF1F2430);
  static const Color darkInk = Color(0xFFE7E9F1);
  static const Color darkMuted = Color(0xFF94A3B8);
  static const Color darkBorder = Color(0xFF2A2F3A);
}

/// Project semantic colours that Material's [ColorScheme] does not model
/// (success / warning / info families + the brand gradient). Exposed as a
/// [ThemeExtension] so it adapts automatically to light/dark and is read via
/// `Theme.of(context).extension<AppSemantics>()` / `context.semantics`.
@immutable
class AppSemantics extends ThemeExtension<AppSemantics> {
  const AppSemantics({
    required this.success,
    required this.onSuccess,
    required this.successContainer,
    required this.onSuccessContainer,
    required this.warning,
    required this.onWarning,
    required this.warningContainer,
    required this.onWarningContainer,
    required this.info,
    required this.infoContainer,
    required this.brandGradient,
    required this.onBrand,
    required this.cardShadow,
  });

  final Color success;
  final Color onSuccess;
  final Color successContainer;
  final Color onSuccessContainer;
  final Color warning;
  final Color onWarning;
  final Color warningContainer;
  final Color onWarningContainer;
  final Color info;
  final Color infoContainer;

  /// The hero/header brand gradient (indigo → deep indigo).
  final Gradient brandGradient;

  /// Foreground colour to use on top of [brandGradient].
  final Color onBrand;

  /// Soft elevation shadow for floating cards (kept subtle for the flat-modern look).
  final List<BoxShadow> cardShadow;

  static const AppSemantics light = AppSemantics(
    success: AppPalette.emerald,
    onSuccess: AppPalette.white,
    successContainer: Color(0xFFD1FAE5),
    onSuccessContainer: Color(0xFF064E3B),
    warning: AppPalette.amber,
    onWarning: AppPalette.white,
    warningContainer: Color(0xFFFEF3C7),
    onWarningContainer: Color(0xFF78350F),
    info: AppPalette.sky,
    infoContainer: Color(0xFFDBEAFE),
    brandGradient: LinearGradient(
      begin: Alignment.topLeft,
      end: Alignment.bottomRight,
      colors: [AppPalette.indigo, AppPalette.indigoDeep],
    ),
    onBrand: AppPalette.white,
    cardShadow: [
      BoxShadow(
        color: Color(0x14111827), // slate-900 @ 8%
        blurRadius: 18,
        offset: Offset(0, 8),
      ),
    ],
  );

  static const AppSemantics dark = AppSemantics(
    success: AppPalette.emeraldLight,
    onSuccess: Color(0xFF052E1B),
    successContainer: Color(0xFF064E3B),
    onSuccessContainer: Color(0xFFD1FAE5),
    warning: Color(0xFFFBBF24),
    onWarning: Color(0xFF422006),
    warningContainer: Color(0xFF4A2E07),
    onWarningContainer: Color(0xFFFDE68A),
    info: Color(0xFF60A5FA),
    infoContainer: Color(0xFF1E3A5F),
    brandGradient: LinearGradient(
      begin: Alignment.topLeft,
      end: Alignment.bottomRight,
      colors: [AppPalette.indigoDeep, Color(0xFF3730A3)],
    ),
    onBrand: AppPalette.white,
    cardShadow: [
      BoxShadow(
        color: Color(0x33000000),
        blurRadius: 18,
        offset: Offset(0, 8),
      ),
    ],
  );

  @override
  AppSemantics copyWith({
    Color? success,
    Color? onSuccess,
    Color? successContainer,
    Color? onSuccessContainer,
    Color? warning,
    Color? onWarning,
    Color? warningContainer,
    Color? onWarningContainer,
    Color? info,
    Color? infoContainer,
    Gradient? brandGradient,
    Color? onBrand,
    List<BoxShadow>? cardShadow,
  }) {
    return AppSemantics(
      success: success ?? this.success,
      onSuccess: onSuccess ?? this.onSuccess,
      successContainer: successContainer ?? this.successContainer,
      onSuccessContainer: onSuccessContainer ?? this.onSuccessContainer,
      warning: warning ?? this.warning,
      onWarning: onWarning ?? this.onWarning,
      warningContainer: warningContainer ?? this.warningContainer,
      onWarningContainer: onWarningContainer ?? this.onWarningContainer,
      info: info ?? this.info,
      infoContainer: infoContainer ?? this.infoContainer,
      brandGradient: brandGradient ?? this.brandGradient,
      onBrand: onBrand ?? this.onBrand,
      cardShadow: cardShadow ?? this.cardShadow,
    );
  }

  @override
  AppSemantics lerp(ThemeExtension<AppSemantics>? other, double t) {
    if (other is! AppSemantics) return this;
    return AppSemantics(
      success: Color.lerp(success, other.success, t)!,
      onSuccess: Color.lerp(onSuccess, other.onSuccess, t)!,
      successContainer: Color.lerp(successContainer, other.successContainer, t)!,
      onSuccessContainer:
          Color.lerp(onSuccessContainer, other.onSuccessContainer, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      onWarning: Color.lerp(onWarning, other.onWarning, t)!,
      warningContainer: Color.lerp(warningContainer, other.warningContainer, t)!,
      onWarningContainer:
          Color.lerp(onWarningContainer, other.onWarningContainer, t)!,
      info: Color.lerp(info, other.info, t)!,
      infoContainer: Color.lerp(infoContainer, other.infoContainer, t)!,
      brandGradient: Gradient.lerp(brandGradient, other.brandGradient, t)!,
      onBrand: Color.lerp(onBrand, other.onBrand, t)!,
      cardShadow: t < 0.5 ? cardShadow : other.cardShadow,
    );
  }
}

/// Ergonomic access: `context.semantics.success`, `context.scheme.primary`.
extension AppThemeContextX on BuildContext {
  AppSemantics get semantics =>
      Theme.of(this).extension<AppSemantics>() ?? AppSemantics.light;
  ColorScheme get scheme => Theme.of(this).colorScheme;
  TextTheme get texts => Theme.of(this).textTheme;
}
