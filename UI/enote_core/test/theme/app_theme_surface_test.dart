import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/theme/app_theme.dart';

void main() {
  test('surfaceContainer is visibly lighter than background', () {
    final difference =
        AppTheme.surfaceContainer.computeLuminance() -
        AppTheme.background.computeLuminance();

    expect(difference, greaterThanOrEqualTo(0.01));
  });

  test('outline is visibly lighter than surfaceContainer', () {
    final difference =
        AppTheme.outline.computeLuminance() -
        AppTheme.surfaceContainer.computeLuminance();

    expect(difference, greaterThanOrEqualTo(0.01));
  });
}
