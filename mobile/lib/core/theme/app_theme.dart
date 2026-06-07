import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_palette.dart';

/// Centralised Material 3 theme for the RMMS 2026 mobile app — **Redesign 2026**.
///
/// Visual language: bold indigo → violet brand, mesh-gradient hero blocks,
/// rounded "bento" cards (radius 26–28), pill primary buttons (radius 17,
/// height 54), Space Grotesk display + Plus Jakarta Sans body, soft layered
/// shadows. Semantic colours / gradients / shadows live in [AppSemantics].
class AppTheme {
  AppTheme._();

  // Backward-compatible constants.
  static const Color kPrimary = AppPalette.indigo;
  static const Color kError = AppPalette.rose;
  static const Color kSuccess = AppPalette.emerald;
  static const Color kWarning = AppPalette.amber;

  // Radius scale (from README design tokens).
  static const double radiusSm = 14;
  static const double radiusMd = 17; // primary buttons
  static const double radiusLg = 22; // list rows / quick cards
  static const double radiusXl = 28; // hero / feature cards
  static const double controlHeight = 54;

  static ThemeData get light => _build(Brightness.light);
  static ThemeData get dark => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;

    final scheme = ColorScheme.fromSeed(
      seedColor: AppPalette.indigo,
      brightness: brightness,
    ).copyWith(
      primary: isDark ? AppPalette.indigoBright : AppPalette.indigo,
      onPrimary: AppPalette.white,
      secondary: AppPalette.violet,
      tertiary: AppPalette.emerald,
      error: isDark ? const Color(0xFFFB7185) : AppPalette.rose,
      surface: isDark ? AppPalette.darkSurface : AppPalette.surface,
      onSurface: isDark ? AppPalette.darkInk : AppPalette.ink,
      surfaceContainerHighest:
          isDark ? AppPalette.darkSurfaceHi : AppPalette.surface2,
      onSurfaceVariant: isDark ? AppPalette.darkMuted : AppPalette.muted,
      outline: isDark ? AppPalette.darkBorder : AppPalette.line,
      outlineVariant: isDark ? AppPalette.darkBorder : AppPalette.line,
    );

    final semantics = isDark ? AppSemantics.dark : AppSemantics.light;
    final scaffoldBg = isDark ? AppPalette.darkBg : AppPalette.bg;
    final base = ThemeData(useMaterial3: true, brightness: brightness);

    final textTheme = _textTheme(base.textTheme, scheme);

    return base.copyWith(
      colorScheme: scheme,
      scaffoldBackgroundColor: scaffoldBg,
      extensions: <ThemeExtension<dynamic>>[semantics],
      textTheme: textTheme,
      splashFactory: InkSparkle.splashFactory,
      appBarTheme: AppBarTheme(
        backgroundColor: scaffoldBg,
        foregroundColor: scheme.onSurface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: GoogleFonts.plusJakartaSans(
          color: scheme.onSurface,
          fontSize: 19,
          fontWeight: FontWeight.w700,
          letterSpacing: -0.2,
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: scheme.surface,
        surfaceTintColor: Colors.transparent,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusXl),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(0, controlHeight),
          backgroundColor: scheme.primary,
          foregroundColor: AppPalette.white,
          textStyle: GoogleFonts.plusJakartaSans(
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMd),
          ),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          minimumSize: const Size(0, controlHeight),
          elevation: 0,
          textStyle: GoogleFonts.plusJakartaSans(
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMd),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(0, controlHeight),
          side: BorderSide(color: scheme.outline),
          textStyle: GoogleFonts.plusJakartaSans(
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radiusMd),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          textStyle: GoogleFonts.plusJakartaSans(
            fontSize: 15,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isDark ? AppPalette.darkSurfaceHi : AppPalette.surface2,
        isDense: false,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radiusMd),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radiusMd),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radiusMd),
          borderSide: BorderSide(color: scheme.primary, width: 1.6),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radiusMd),
          borderSide: BorderSide(color: scheme.error, width: 1.2),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radiusMd),
          borderSide: BorderSide(color: scheme.error, width: 1.6),
        ),
        labelStyle: GoogleFonts.plusJakartaSans(color: scheme.onSurfaceVariant),
        floatingLabelStyle: GoogleFonts.plusJakartaSans(color: scheme.primary),
        hintStyle: GoogleFonts.plusJakartaSans(
          color: isDark ? AppPalette.darkMuted : AppPalette.faint,
          fontWeight: FontWeight.w500,
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: scheme.surfaceContainerHighest,
        side: BorderSide.none,
        shape: const StadiumBorder(),
        labelStyle: GoogleFonts.plusJakartaSans(
          color: scheme.onSurfaceVariant,
          fontWeight: FontWeight.w700,
          fontSize: 12.5,
        ),
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 4),
      ),
      listTileTheme: ListTileThemeData(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusLg),
        ),
        iconColor: scheme.onSurfaceVariant,
      ),
      dividerTheme: DividerThemeData(
        color: scheme.outlineVariant,
        thickness: 1,
        space: 1,
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.transparent,
        elevation: 0,
        height: 70,
        indicatorColor: Colors.transparent,
        labelTextStyle: WidgetStatePropertyAll(
          GoogleFonts.plusJakartaSans(
            fontSize: 10.5,
            fontWeight: FontWeight.w700,
            color: scheme.onSurfaceVariant,
          ),
        ),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
        ),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusXl),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: isDark ? AppPalette.darkSurfaceHi : AppPalette.ink,
        contentTextStyle: GoogleFonts.plusJakartaSans(
          color: AppPalette.white,
          fontWeight: FontWeight.w600,
        ),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radiusMd),
        ),
      ),
      progressIndicatorTheme:
          ProgressIndicatorThemeData(color: scheme.primary),
    );
  }

  /// Composed text theme — **Plus Jakarta Sans** for body/UI, **Space Grotesk**
  /// for display roles (page titles, big clock, hero numbers).
  static TextTheme _textTheme(TextTheme base, ColorScheme scheme) {
    final ink = scheme.onSurface;
    final muted = scheme.onSurfaceVariant;

    // Apply Plus Jakarta Sans across the whole body theme first.
    final body = GoogleFonts.plusJakartaSansTextTheme(base).apply(
      bodyColor: ink,
      displayColor: ink,
    );

    // Then override display roles with Space Grotesk.
    final displayTtfg = GoogleFonts.spaceGrotesk(
      color: ink,
      fontWeight: FontWeight.w800,
      letterSpacing: -0.8,
    );
    final displayTtfg2 = GoogleFonts.spaceGrotesk(
      color: ink,
      fontWeight: FontWeight.w700,
      letterSpacing: -0.6,
    );

    return body.copyWith(
      displayLarge: body.displayLarge?.merge(displayTtfg),
      displayMedium: body.displayMedium?.merge(displayTtfg),
      displaySmall: body.displaySmall?.merge(displayTtfg),
      headlineLarge: body.headlineLarge?.merge(displayTtfg),
      headlineMedium: body.headlineMedium?.merge(displayTtfg2),
      headlineSmall: body.headlineSmall?.merge(displayTtfg2),
      titleLarge: body.titleLarge?.copyWith(
        fontWeight: FontWeight.w800,
        letterSpacing: -0.3,
      ),
      titleMedium: body.titleMedium?.copyWith(fontWeight: FontWeight.w700),
      titleSmall: body.titleSmall?.copyWith(fontWeight: FontWeight.w700),
      bodyLarge: body.bodyLarge?.copyWith(height: 1.45, fontWeight: FontWeight.w500),
      bodyMedium:
          body.bodyMedium?.copyWith(color: ink, height: 1.45, fontWeight: FontWeight.w500),
      bodySmall: body.bodySmall?.copyWith(color: muted, height: 1.4),
      labelLarge: body.labelLarge?.copyWith(fontWeight: FontWeight.w700),
      labelMedium: body.labelMedium?.copyWith(fontWeight: FontWeight.w700),
      labelSmall: body.labelSmall?.copyWith(
        fontWeight: FontWeight.w800,
        letterSpacing: 0.8,
      ),
    );
  }

  /// Convenience: Space Grotesk style for big numbers / clock / hero titles.
  static TextStyle display({
    double size = 26,
    FontWeight weight = FontWeight.w800,
    double letterSpacing = -0.8,
    Color? color,
    double? height,
  }) {
    return GoogleFonts.spaceGrotesk(
      fontSize: size,
      fontWeight: weight,
      letterSpacing: letterSpacing,
      color: color,
      height: height,
    );
  }

  /// Convenience: Plus Jakarta Sans body/UI style.
  static TextStyle body({
    double size = 14.5,
    FontWeight weight = FontWeight.w500,
    Color? color,
    double? height,
    double? letterSpacing,
  }) {
    return GoogleFonts.plusJakartaSans(
      fontSize: size,
      fontWeight: weight,
      color: color,
      height: height,
      letterSpacing: letterSpacing,
    );
  }
}
