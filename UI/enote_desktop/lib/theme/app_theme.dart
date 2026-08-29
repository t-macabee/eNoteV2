import 'package:flutter/material.dart';

/// App-wide dark theme, calibrated against the "Instrumenti" mockup.
class AppTheme {
  AppTheme._();

  static const Color background = Color(0xFF0B0B0D);
  static const Color surfaceContainer = Color(0xFF141416);
  static const Color outline = Color(0xFF232327);
  static const Color primary = Color(0xFFE8863F);
  static const Color onPrimary = Colors.white;
  static const Color textPrimary = Color(0xFFF5F5F6);
  static const Color textSecondary = Color(0xFF9CA3AF);
  static const Color textTertiary = Color(0xFF6B7280);
  static const Color success = Color(0xFF22C55E);
  static const Color warning = Color(0xFFF2A65A);
  static const Color error = Color(0xFFEF4444);

  static ThemeData get dark {
    final colorScheme = ColorScheme.fromSeed(
      seedColor: primary,
      brightness: Brightness.dark,
    ).copyWith(
      primary: primary,
      onPrimary: onPrimary,
      surface: background,
      onSurface: textPrimary,
      onSurfaceVariant: textSecondary,
      outline: outline,
      outlineVariant: outline,
      surfaceContainerLowest: surfaceContainer,
      surfaceContainerLow: surfaceContainer,
      surfaceContainer: surfaceContainer,
      surfaceContainerHigh: surfaceContainer,
      surfaceContainerHighest: surfaceContainer,
      error: error,
    );

    final baseTextTheme = ThemeData(
      brightness: Brightness.dark,
      colorScheme: colorScheme,
    ).textTheme;

    final textTheme = baseTextTheme.copyWith(
      headlineSmall: baseTextTheme.headlineSmall?.copyWith(
        color: textPrimary,
        fontWeight: FontWeight.w600,
      ),
      titleLarge: baseTextTheme.titleLarge?.copyWith(
        color: textPrimary,
        fontWeight: FontWeight.w600,
      ),
      titleMedium: baseTextTheme.titleMedium?.copyWith(
        color: textPrimary,
        fontWeight: FontWeight.w500,
      ),
      labelSmall: baseTextTheme.labelSmall?.copyWith(
        color: textTertiary,
        letterSpacing: 1.2,
      ),
      labelMedium: baseTextTheme.labelMedium?.copyWith(
        color: textTertiary,
      ),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: background,
      textTheme: textTheme,
      appBarTheme: const AppBarTheme(
        backgroundColor: background,
        foregroundColor: textPrimary,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: primary,
          foregroundColor: onPrimary,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primary,
          foregroundColor: onPrimary,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surfaceContainer,
        prefixIconColor: textSecondary,
        suffixIconColor: textSecondary,
        labelStyle: const TextStyle(color: textSecondary),
        hintStyle: const TextStyle(color: textTertiary),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outline),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outline),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: primary),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: error),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: outline,
        thickness: 1,
      ),
      listTileTheme: const ListTileThemeData(
        iconColor: textSecondary,
        selectedColor: primary,
        selectedTileColor: Color(0x1FE8863F),
      ),
      iconTheme: const IconThemeData(color: textSecondary),
      progressIndicatorTheme: const ProgressIndicatorThemeData(
        color: primary,
      ),
    );
  }
}
