import 'package:flutter/material.dart';

import 'package:enote_core/theme/app_theme.dart';
import 'package:enote_core/theme/app_tokens.dart';

export 'package:enote_core/theme/app_theme.dart';

TextTheme _mobileTextTheme(TextTheme base) {
  final roboto = base.apply(fontFamily: 'Roboto', package: 'enote_core');
  return roboto.copyWith(
    displaySmall: roboto.displaySmall?.copyWith(
      fontSize: 36,
      fontWeight: FontWeight.w600,
    ),
    headlineSmall: roboto.headlineSmall?.copyWith(
      fontSize: 24,
      fontWeight: FontWeight.w600,
    ),
    titleLarge: roboto.titleLarge?.copyWith(
      fontSize: 22,
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
    bodyLarge: roboto.bodyLarge?.copyWith(
      fontSize: 16,
      fontWeight: FontWeight.w400,
    ),
    bodyMedium: roboto.bodyMedium?.copyWith(
      fontSize: 14,
      fontWeight: FontWeight.w400,
    ),
    bodySmall: roboto.bodySmall?.copyWith(
      fontSize: 12,
      fontWeight: FontWeight.w400,
    ),
    labelLarge: roboto.labelLarge?.copyWith(
      fontSize: 14,
      fontWeight: FontWeight.w500,
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

final ButtonStyle dangerOutlinedButtonStyle = OutlinedButton.styleFrom(
  foregroundColor: AppTheme.error,
  side: const BorderSide(color: AppTheme.error),
);

final ThemeData mobileTheme = AppTheme.dark.copyWith(
  textTheme: _mobileTextTheme(AppTheme.dark.textTheme),
  primaryTextTheme: _mobileTextTheme(AppTheme.dark.primaryTextTheme),
  bottomSheetTheme: BottomSheetThemeData(
    showDragHandle: true,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
  ),
);
