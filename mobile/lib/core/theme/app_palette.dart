import 'package:flutter/material.dart';

/// Raw design tokens for the RMMS 2026 mobile theme — **Redesign 2026**.
///
/// Visual language: bolder indigo → violet brand with mesh-gradient hero blocks,
/// rounded "bento" cards, large display typography, and tasteful micro-motion.
/// Tokens live here; widgets read them through [ColorScheme] or the
/// [AppSemantics] ThemeExtension, never as ad-hoc hex.
class AppPalette {
  AppPalette._();

  // ---- Brand (indigo → violet) -----------------------------------------
  static const Color indigo = Color(0xFF5B5BF0); // primary brand
  static const Color indigoDeep = Color(0xFF4338CA); // gradient end / pressed
  static const Color indigoBright = Color(0xFF7C7CFF); // gradient start / secondary
  static const Color indigoLight = indigoBright; // legacy alias
  static const Color violet = Color(0xFF8B5CF6); // mesh accent

  // ---- Semantic accents -------------------------------------------------
  static const Color emerald = Color(0xFF10B981);
  static const Color emeraldDeep = Color(0xFF059669); // text on emerald container
  static const Color emeraldBright = Color(0xFF34D399); // emerald gradient start
  static const Color emeraldLight = emeraldBright; // legacy alias
  static const Color emeraldSoft = Color(0xFFECFDF5);
  static const Color amber = Color(0xFFF59E0B);
  static const Color rose = Color(0xFFF43F5E);
  static const Color red = rose; // legacy alias
  static const Color sky = Color(0xFF0EA5E9);

  // ---- Neutrals (cool, slightly warm) -----------------------------------
  static const Color ink = Color(0xFF161528);
  static const Color body = Color(0xFF45445C);
  static const Color muted = Color(0xFF8A89A3);
  static const Color faint = Color(0xFFB6B5CC);
  static const Color line = Color(0xFFECECF3);
  static const Color border = line; // legacy alias
  static const Color surface = Color(0xFFFFFFFF);
  static const Color white = surface;
  static const Color surface2 = Color(0xFFF5F5FB); // input fill / chips / soft buttons
  static const Color surfaceMuted = surface2; // legacy alias
  static const Color bg = Color(0xFFF1F1F8); // scaffold
  static const Color indigoTintBg = bg; // legacy alias

  // ---- Status chip pairs (bg / fg) -------------------------------------
  static const Color chipNeutralBg = Color(0xFFF1F1F8);
  static const Color chipNeutralFg = Color(0xFF6E6D87);
  static const Color chipIndigoBg = Color(0xFFEEEEFF);
  static const Color chipIndigoFg = Color(0xFF4338CA);
  static const Color chipEmeraldBg = Color(0xFFE7FBF2);
  static const Color chipEmeraldFg = Color(0xFF059669);
  static const Color chipAmberBg = Color(0xFFFFF6E5);
  static const Color chipAmberFg = Color(0xFFB45309);
  static const Color chipRoseBg = Color(0xFFFFEEF1);
  static const Color chipRoseFg = Color(0xFFE11D48);
  static const Color chipSkyBg = Color(0xFFE6F5FE);
  static const Color chipSkyFg = Color(0xFF0284C7);

  // ---- IconTile soft backgrounds ---------------------------------------
  static const Color tileIndigoBg = Color(0xFFEEEEFF);
  static const Color tileIndigoFg = Color(0xFF5B5BF0);
  static const Color tileVioletBg = Color(0xFFF3ECFF);
  static const Color tileVioletFg = Color(0xFF8B5CF6);
  static const Color tileEmeraldBg = Color(0xFFE7FBF2);
  static const Color tileEmeraldFg = Color(0xFF059669);
  static const Color tileAmberBg = Color(0xFFFFF4E0);
  static const Color tileAmberFg = Color(0xFFEA9009);
  static const Color tileSkyBg = Color(0xFFE4F4FE);
  static const Color tileSkyFg = Color(0xFF0EA5E9);
  static const Color tileRoseBg = Color(0xFFFFECF0);
  static const Color tileRoseFg = Color(0xFFF43F5E);

  // ---- Dark neutrals ----------------------------------------------------
  static const Color darkBg = Color(0xFF0F1117);
  static const Color darkSurface = Color(0xFF171A21);
  static const Color darkSurfaceHi = Color(0xFF1F2430);
  static const Color darkInk = Color(0xFFE7E9F1);
  static const Color darkMuted = Color(0xFF94A3B8);
  static const Color darkBorder = Color(0xFF2A2F3A);
}

/// Project semantic colours, gradients, radii and shadows that Material's
/// [ColorScheme] does not model. Exposed as a [ThemeExtension] so it adapts
/// automatically to light/dark and is read via `context.semantics`.
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
    required this.danger,
    required this.dangerContainer,
    required this.onDangerContainer,
    required this.brandGradient,
    required this.meshGradient,
    required this.emeraldGradient,
    required this.onBrand,
    required this.cardShadow,
    required this.shadowSm,
    required this.shadowLg,
    required this.shadowBrand,
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
  final Color danger;
  final Color dangerContainer;
  final Color onDangerContainer;

  /// Primary brand gradient (135°, #7C7CFF → #5B5BF0 → #4338CA).
  final Gradient brandGradient;

  /// Mesh gradient approximation used for hero / header blocks.
  /// Approximated as a linear sweep since radial layers would need a Stack.
  final Gradient meshGradient;

  /// Success / checked-in gradient (135°, #34D399 → #059669).
  final Gradient emeraldGradient;

  /// Foreground colour on top of brand gradients.
  final Color onBrand;

  /// Default soft elevation for surface cards (alias of shadowSm).
  final List<BoxShadow> cardShadow;
  final List<BoxShadow> shadowSm;
  final List<BoxShadow> shadowLg;

  /// Glow under gradient buttons / FAB.
  final List<BoxShadow> shadowBrand;

  static const Gradient _brandGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    stops: [0.0, 0.48, 1.0],
    colors: [
      AppPalette.indigoBright,
      AppPalette.indigo,
      AppPalette.indigoDeep,
    ],
  );

  static const Gradient _meshGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    stops: [0.0, 0.55, 1.0],
    colors: [
      AppPalette.violet,
      AppPalette.indigo,
      AppPalette.indigoDeep,
    ],
  );

  static const Gradient _emeraldGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [AppPalette.emeraldBright, AppPalette.emeraldDeep],
  );

  static const List<BoxShadow> _shadowSmLight = [
    BoxShadow(
      color: Color(0x0A161528), // rgba(22,21,40,.04)
      blurRadius: 2,
      offset: Offset(0, 1),
    ),
    BoxShadow(
      color: Color(0x0A161528),
      blurRadius: 12,
      offset: Offset(0, 4),
    ),
  ];

  static const List<BoxShadow> _shadowLight = [
    BoxShadow(
      color: Color(0x0D161528), // rgba(22,21,40,.05)
      blurRadius: 6,
      offset: Offset(0, 2),
    ),
    BoxShadow(
      color: Color(0x12161528), // rgba(22,21,40,.07)
      blurRadius: 36,
      offset: Offset(0, 14),
    ),
  ];

  static const List<BoxShadow> _shadowLgLight = [
    BoxShadow(
      color: Color(0x294338CA), // rgba(67,56,202,.16)
      blurRadius: 24,
      offset: Offset(0, 8),
    ),
    BoxShadow(
      color: Color(0x244338CA), // rgba(67,56,202,.14)
      blurRadius: 56,
      offset: Offset(0, 24),
    ),
  ];

  static const List<BoxShadow> _shadowBrandLight = [
    BoxShadow(
      color: Color(0x615B5BF0), // rgba(91,91,240,.38)
      blurRadius: 30,
      offset: Offset(0, 10),
    ),
  ];

  static const AppSemantics light = AppSemantics(
    success: AppPalette.emerald,
    onSuccess: AppPalette.white,
    successContainer: AppPalette.chipEmeraldBg,
    onSuccessContainer: AppPalette.chipEmeraldFg,
    warning: AppPalette.amber,
    onWarning: AppPalette.white,
    warningContainer: AppPalette.chipAmberBg,
    onWarningContainer: AppPalette.chipAmberFg,
    info: AppPalette.sky,
    infoContainer: AppPalette.chipSkyBg,
    danger: AppPalette.rose,
    dangerContainer: AppPalette.chipRoseBg,
    onDangerContainer: AppPalette.chipRoseFg,
    brandGradient: _brandGradient,
    meshGradient: _meshGradient,
    emeraldGradient: _emeraldGradient,
    onBrand: AppPalette.white,
    cardShadow: _shadowSmLight,
    shadowSm: _shadowSmLight,
    shadowLg: _shadowLight,
    shadowBrand: _shadowBrandLight,
  );

  static const AppSemantics dark = AppSemantics(
    success: AppPalette.emeraldBright,
    onSuccess: Color(0xFF052E1B),
    successContainer: Color(0xFF064E3B),
    onSuccessContainer: Color(0xFFD1FAE5),
    warning: Color(0xFFFBBF24),
    onWarning: Color(0xFF422006),
    warningContainer: Color(0xFF4A2E07),
    onWarningContainer: Color(0xFFFDE68A),
    info: Color(0xFF60A5FA),
    infoContainer: Color(0xFF1E3A5F),
    danger: Color(0xFFFB7185),
    dangerContainer: Color(0xFF4A1620),
    onDangerContainer: Color(0xFFFFD4DA),
    brandGradient: _brandGradient,
    meshGradient: _meshGradient,
    emeraldGradient: _emeraldGradient,
    onBrand: AppPalette.white,
    cardShadow: [
      BoxShadow(color: Color(0x33000000), blurRadius: 18, offset: Offset(0, 8)),
    ],
    shadowSm: [
      BoxShadow(color: Color(0x33000000), blurRadius: 12, offset: Offset(0, 4)),
    ],
    shadowLg: [
      BoxShadow(color: Color(0x66000000), blurRadius: 36, offset: Offset(0, 14)),
    ],
    shadowBrand: [
      BoxShadow(
        color: Color(0x664338CA),
        blurRadius: 30,
        offset: Offset(0, 10),
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
    Color? danger,
    Color? dangerContainer,
    Color? onDangerContainer,
    Gradient? brandGradient,
    Gradient? meshGradient,
    Gradient? emeraldGradient,
    Color? onBrand,
    List<BoxShadow>? cardShadow,
    List<BoxShadow>? shadowSm,
    List<BoxShadow>? shadowLg,
    List<BoxShadow>? shadowBrand,
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
      danger: danger ?? this.danger,
      dangerContainer: dangerContainer ?? this.dangerContainer,
      onDangerContainer: onDangerContainer ?? this.onDangerContainer,
      brandGradient: brandGradient ?? this.brandGradient,
      meshGradient: meshGradient ?? this.meshGradient,
      emeraldGradient: emeraldGradient ?? this.emeraldGradient,
      onBrand: onBrand ?? this.onBrand,
      cardShadow: cardShadow ?? this.cardShadow,
      shadowSm: shadowSm ?? this.shadowSm,
      shadowLg: shadowLg ?? this.shadowLg,
      shadowBrand: shadowBrand ?? this.shadowBrand,
    );
  }

  @override
  AppSemantics lerp(ThemeExtension<AppSemantics>? other, double t) {
    if (other is! AppSemantics) return this;
    return AppSemantics(
      success: Color.lerp(success, other.success, t)!,
      onSuccess: Color.lerp(onSuccess, other.onSuccess, t)!,
      successContainer:
          Color.lerp(successContainer, other.successContainer, t)!,
      onSuccessContainer:
          Color.lerp(onSuccessContainer, other.onSuccessContainer, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      onWarning: Color.lerp(onWarning, other.onWarning, t)!,
      warningContainer:
          Color.lerp(warningContainer, other.warningContainer, t)!,
      onWarningContainer:
          Color.lerp(onWarningContainer, other.onWarningContainer, t)!,
      info: Color.lerp(info, other.info, t)!,
      infoContainer: Color.lerp(infoContainer, other.infoContainer, t)!,
      danger: Color.lerp(danger, other.danger, t)!,
      dangerContainer: Color.lerp(dangerContainer, other.dangerContainer, t)!,
      onDangerContainer:
          Color.lerp(onDangerContainer, other.onDangerContainer, t)!,
      brandGradient: Gradient.lerp(brandGradient, other.brandGradient, t)!,
      meshGradient: Gradient.lerp(meshGradient, other.meshGradient, t)!,
      emeraldGradient:
          Gradient.lerp(emeraldGradient, other.emeraldGradient, t)!,
      onBrand: Color.lerp(onBrand, other.onBrand, t)!,
      cardShadow: t < 0.5 ? cardShadow : other.cardShadow,
      shadowSm: t < 0.5 ? shadowSm : other.shadowSm,
      shadowLg: t < 0.5 ? shadowLg : other.shadowLg,
      shadowBrand: t < 0.5 ? shadowBrand : other.shadowBrand,
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
