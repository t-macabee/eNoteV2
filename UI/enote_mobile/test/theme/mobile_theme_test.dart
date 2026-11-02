import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/theme/app_theme.dart';

double _textWidth(FontWeight weight) {
  final painter = TextPainter(
    text: TextSpan(
      text: 'Odobri upis studenta',
      style: TextStyle(
        fontFamily: 'packages/enote_core/Roboto',
        fontSize: 16,
        fontWeight: weight,
      ),
    ),
    textDirection: TextDirection.ltr,
  )..layout();
  return painter.width;
}

void main() {
  test('mobile type scale matches the table', () {
    final textTheme = mobileTheme.textTheme;

    void expectRole(TextStyle? style, double fontSize, FontWeight weight) {
      expect(style?.fontSize, fontSize);
      expect(style?.fontWeight, weight);
      expect(style?.fontFamily, 'packages/enote_core/Roboto');
    }

    expectRole(textTheme.displaySmall, 36, FontWeight.w600);
    expectRole(textTheme.headlineSmall, 24, FontWeight.w600);
    expectRole(textTheme.titleLarge, 22, FontWeight.w600);
    expectRole(textTheme.titleMedium, 16, FontWeight.w600);
    expectRole(textTheme.titleSmall, 14, FontWeight.w600);
    expectRole(textTheme.bodyLarge, 16, FontWeight.w400);
    expectRole(textTheme.bodyMedium, 14, FontWeight.w400);
    expectRole(textTheme.bodySmall, 12, FontWeight.w400);
    expectRole(textTheme.labelLarge, 14, FontWeight.w500);
    expectRole(textTheme.labelMedium, 12, FontWeight.w500);
    expectRole(textTheme.labelSmall, 11, FontWeight.w500);
  });

  test('w600 text resolves to the SemiBold face', () {
    final semiBold = _textWidth(FontWeight.w600);
    final bold = _textWidth(FontWeight.w700);

    expect(semiBold, lessThan(bold));
  });
}
