import 'package:flutter/material.dart';

import 'package:enote_core/theme/app_theme.dart';

export 'package:enote_core/theme/app_theme.dart';

TextTheme _desktopTextTheme(TextTheme base) {
  final roboto = base.apply(fontFamily: 'Roboto');
  return roboto.copyWith(
    headlineMedium: roboto.headlineMedium?.copyWith(
      fontSize: 28,
      fontWeight: FontWeight.w600,
    ),
    titleLarge: roboto.titleLarge?.copyWith(
      fontSize: 18,
      fontWeight: FontWeight.w600,
    ),
    titleMedium: roboto.titleMedium?.copyWith(
      fontSize: 16,
      fontWeight: FontWeight.w600,
    ),
    titleSmall: roboto.titleSmall?.copyWith(
      fontSize: 14,
      fontWeight: FontWeight.w600,
    ),
    bodyMedium: roboto.bodyMedium?.copyWith(
      fontSize: 14,
      fontWeight: FontWeight.w400,
    ),
    bodySmall: roboto.bodySmall?.copyWith(
      fontSize: 13,
      fontWeight: FontWeight.w400,
    ),
    labelMedium: roboto.labelMedium?.copyWith(
      fontSize: 12,
      fontWeight: FontWeight.w500,
    ),
    labelSmall: roboto.labelSmall?.copyWith(
      fontSize: 11,
      fontWeight: FontWeight.w500,
    ),
  );
}

final ThemeData desktopTheme = AppTheme.dark.copyWith(
  textTheme: _desktopTextTheme(AppTheme.dark.textTheme),
  primaryTextTheme: _desktopTextTheme(AppTheme.dark.primaryTextTheme),
);
