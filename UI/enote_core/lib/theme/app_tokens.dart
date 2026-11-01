import 'package:flutter/material.dart';

import 'app_theme.dart';

final class AppSpacing {
  const AppSpacing._();

  static const double xs = 4;
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double xxl = 32;
}

final class AppRadius {
  const AppRadius._();

  static const double sm = 4;
  static const double md = 8;
  static const double lg = 12;
  static const double pill = 999;
}

final class AppSize {
  const AppSize._();

  static const double sidebarWidth = 256;
  static const double contentMaxWidth = 1100;
  static const double formMaxWidth = 720;
  static const double dialogMaxWidth = 640;
  static const double dialogContentWidth = 520;
  static const double tableRowHeight = 48;
  static const double toolbarHeight = 56;
}

enum StatusTone { neutral, info, success, warning, danger, accent }

extension StatusToneStyle on StatusTone {
  Color get color => switch (this) {
    StatusTone.neutral => AppTheme.textSecondary,
    StatusTone.info => const Color(0xFF3B82F6),
    StatusTone.success => AppTheme.success,
    StatusTone.warning => AppTheme.warning,
    StatusTone.danger => AppTheme.error,
    StatusTone.accent => AppTheme.primary,
  };

  Color get background => color.withValues(alpha: 0.15);
}
