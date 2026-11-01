import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  test('StatusTone.success uses AppTheme.success', () {
    expect(StatusTone.success.color, AppTheme.success);
  });

  test('StatusTone background is the color at alpha 0.15', () {
    expect(StatusTone.danger.background.a, closeTo(0.15, 0.01));
  });

  test('AppSpacing values rise in order', () {
    final values = [
      AppSpacing.xs,
      AppSpacing.sm,
      AppSpacing.md,
      AppSpacing.lg,
      AppSpacing.xl,
      AppSpacing.xxl,
    ];
    for (var i = 1; i < values.length; i++) {
      expect(values[i], greaterThan(values[i - 1]));
    }
  });
}
