import 'package:flutter/material.dart';

/// Centralized theme constants. Keep colors aligned with Web (AntD primary #1677FF).
class AppTheme {
  AppTheme._();

  static const Color kPrimary = Color(0xFF1677FF);
  static const Color kError = Color(0xFFFF4D4F);
  static const Color kSuccess = Color(0xFF52C41A);
  static const Color kWarning = Color(0xFFFAAD14);

  static ThemeData get light => ThemeData(
    useMaterial3: true,
    colorSchemeSeed: kPrimary,
    brightness: Brightness.light,
    appBarTheme: const AppBarTheme(centerTitle: true, elevation: 0),
    inputDecorationTheme: const InputDecorationTheme(
      border: OutlineInputBorder(),
      isDense: true,
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
    ),
  );

  static ThemeData get dark => ThemeData(
    useMaterial3: true,
    colorSchemeSeed: kPrimary,
    brightness: Brightness.dark,
  );
}
